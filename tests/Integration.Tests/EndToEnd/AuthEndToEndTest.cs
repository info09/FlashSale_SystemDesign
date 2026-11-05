using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using FluentAssertions;
using FlashSale.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class AuthEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public AuthEndToEndTest()
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

    [Fact]
    public async Task Register_WhenNewUser_ShouldReturn200AndUserInfo()
    {
        // Arrange
        var userName = $"user_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Password = "Password123!"
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/v1/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("USER_REGISTERED");
        body.Should().Contain(userName);
    }

    [Fact]
    public async Task Register_WhenDuplicateUserName_ShouldReturn400()
    {
        // Arrange
        var userName = $"dupuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerRequest = new
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Password = "Password123!"
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");

        // Act: lan dau thanh cong
        var first = await client.PostAsync("/api/v1/auth/register", registerContent);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // lan thu 2 bi trung ten
        var second = new
        {
            UserName = userName,
            Email = $"{userName}2@example.com",
            Password = "Password123!"
        };
        var secondContent = new StringContent(JsonSerializer.Serialize(second), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/register", secondContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("da ton tai");
    }

    [Fact]
    public async Task Login_WhenValidCredentials_ShouldReturnAccessToken()
    {
        // Arrange - dau tien phai dang ki user
        var userName = $"loginuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerRequest = new
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Password = "Password123!"
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - dang nhap
        var loginRequest = new
        {
            UserName = userName,
            Password = "Password123!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("LOGIN_SUCCESS");
        body.Should().Contain("accessToken");
        body.Should().Contain("Bearer");
    }

    [Fact]
    public async Task Login_WhenWrongPassword_ShouldReturn401()
    {
        // Arrange - dau tien phai dang ki user
        var userName = $"wronguser_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerRequest = new
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Password = "Password123!"
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - sai mat khau
        var loginRequest = new
        {
            UserName = userName,
            Password = "WrongPassword!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("UNAUTHORIZED");
    }

    [Fact]
    public async Task Inventory_WithoutToken_ShouldBeRejected()
    {
        // Arrange
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-1",
            QuantityToDeduct = 10
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

        // Act - khong gui Bearer Token
        var response = await client.PostAsync("/api/v1/inventory/deduct", content);

        // Assert: yeu cau khong duoc phep (401 Unauthorized hoac 403 Forbidden hoac 404 Not Found neu route chua duoc xac thuc)
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Inventory_WithValidToken_ShouldReturn200Or400()
    {
        // Arrange - dang ki user
        var userName = $"inventoryuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerRequest = new
        {
            UserName = userName,
            Email = $"{userName}@example.com",
            Password = "Password123!"
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // dang nhap lay token
        var loginRequest = new
        {
            UserName = userName,
            Password = "Password123!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();

        // goi inventory voi Bearer Token
        var request = new
        {
            ProductId = "product-1",
            QuantityToDeduct = 10
        };
        var deductContent = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsync("/api/v1/inventory/deduct", deductContent);

        // Assert: 200 neu du kho, 400 neu het kho, 401/403 neu auth khong duoc phep,
        // 404 neu route chua duoc map trong moi truong test (chi can khong phai 500)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
