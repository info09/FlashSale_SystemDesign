using System.Net;
using System.Text;
using System.Text.Json;
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
                });
            });
        });
    }

    [Fact]
    public async Task GivenEnoughInventory_WhenDeduct_ShouldReturn200Ok()
    {
        // Arrange - Set inventory ban đầu qua repository (Singleton)
        var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        await inventoryRepository.SetInventoryAsync("product-1", 100);

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-1",
            QuantityToDeduct = 10
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

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
        // Arrange - Set inventory thấp, yêu cầu deduct vượt quá tồn kho
        var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        await inventoryRepository.SetInventoryAsync("product-2", 5);

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-2",
            QuantityToDeduct = 100
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/v1/inventory/deduct", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INSUFFICIENT_INVENTORY");
        body.Should().Contain("đã hết hàng");
    }
}
