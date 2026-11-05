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

public class AuthFlowEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public AuthFlowEndToEndTest()
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
    public async Task FullAuthFlow_RegisterLoginAccessProtectedEndpoint_Success()
    {
        // Arrange
        var userName = $"flowuser_{Guid.NewGuid():N}";
        var email = $"{userName}@example.com";
        var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Step 1: Register user
        var registerRequest = new
        {
            UserName = userName,
            Email = email,
            Password = "Password123!"
        };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        registerBody.Should().Contain("USER_REGISTERED");
        registerBody.Should().Contain(userName);

        // Step 2: Login to get JWT Bearer token
        var loginRequest = new
        {
            UserName = userName,
            Password = "Password123!"
        };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        loginBody.Should().Contain("LOGIN_SUCCESS");
        loginBody.Should().Contain("accessToken");
        loginBody.Should().Contain("Bearer");

        var loginDoc = JsonDocument.Parse(loginBody);
        var token = loginDoc.RootElement.GetProperty("accessToken").GetString();
        token.Should().NotBeNullOrEmpty();

        // Step 3: Call protected inventory endpoint WITHOUT token → rejected
        var deductRequest = new
        {
            ProductId = "product-1",
            QuantityToDeduct = 10
        };
        var deductContent = new StringContent(JsonSerializer.Serialize(deductRequest), Encoding.UTF8, "application/json");
        var noTokenResponse = await client.PostAsync("/api/v1/inventory/deduct", deductContent);
        noTokenResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);

        // Step 4: Call protected inventory endpoint WITH valid token → accepted
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        var withTokenResponse = await client.PostAsync("/api/v1/inventory/deduct", deductContent);
        // 200 (du kho) hoac 400 (het kho) la chinh xac; quan trong la khong phai 500 hoac 401
        withTokenResponse.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        withTokenResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
