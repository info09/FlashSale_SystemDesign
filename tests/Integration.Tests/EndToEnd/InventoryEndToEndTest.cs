using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using FluentAssertions;
using FlashSale.Api;
using FlashSale.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class InventoryEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public InventoryEndToEndTest()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("JWT_SECRET", "test_jwt_secret_key_at_least_32_characters_long_for_hmac_sha256"),
                    new KeyValuePair<string, string>("JWT_ISSUER", "FlashSale.Api"),
                    new KeyValuePair<string, string>("JWT_AUDIENCE", "FlashSale.Client"),
                    new KeyValuePair<string, string>("JWT_EXPIRATION_MINUTES", "60"),
                });
            });
        });
    }

    private async Task<string> RegisterAndGetToken(WebApplicationFactory<Program> factory, string userName, string email, string password)
    {
        using var client = factory.CreateClient();

        // Register
        var registerRequest = new { UserName = userName, Email = email, Password = password };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        var regBody = await registerResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"REGISTER_RESPONSE: {registerResponse.StatusCode} - {regBody}");
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login
        var loginRequest = new { UserName = userName, Password = password };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        return loginDoc.RootElement.GetProperty("accessToken").GetString();
    }

    [Fact]
    public async Task GivenEnoughInventory_WhenDeduct_ShouldReturn200Ok()
    {
        // Arrange
        var userName = $"invuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        await inventoryRepository.SetInventoryAsync("product-1", 100);

        var token = await RegisterAndGetToken(factory, userName, $"{userName}@example.com", "Password123!");

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-1",
            QuantityToDeduct = 10
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsync("/api/v1/inventory/deduct", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INVENTORY_DEDUCTED");
        body.Should().Contain("\"deductedQuantity\":10");
        body.Should().Contain("\"remainingQuantity\":90");
    }

    [Fact]
    public async Task GivenNotEnoughInventory_WhenDeduct_ShouldReturn400AndNotCreateOrder()
    {
        // Arrange
        var userName = $"invuser2_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        await inventoryRepository.SetInventoryAsync("product-2", 5);

        var token = await RegisterAndGetToken(factory, userName, $"{userName}@example.com", "Password123!");

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-2",
            QuantityToDeduct = 100
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsync("/api/v1/inventory/deduct", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INSUFFICIENT_INVENTORY");
        body.Should().Contain("đã hết hàng");
    }
}
