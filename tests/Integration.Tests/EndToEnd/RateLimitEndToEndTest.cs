using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class RateLimitEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public RateLimitEndToEndTest()
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

    [Fact]
    public async Task GivenUnderRateLimit_WhenCallApi_ShouldReturn200()
    {
        // Arrange - Set environment variables for Redis connection
        Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RATE_LIMIT_RPS", "100");
        Environment.SetEnvironmentVariable("RATE_LIMIT_WINDOW_SECONDS", "60");

        var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Client-Id", "e2e-client-1");
        request.Headers.Add("Correlation-Id", "e2e-corr-1");
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GivenOverRateLimit_WhenCallApi_ShouldReturn429()
    {
        // Arrange - Set environment variables for Redis connection with low rate limit
        Environment.SetEnvironmentVariable("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString());
        Environment.SetEnvironmentVariable("RATE_LIMIT_RPS", "3");
        Environment.SetEnvironmentVariable("RATE_LIMIT_WINDOW_SECONDS", "60");

        var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var clientId = "e2e-client-2";

        // Act: Gọi quá ngưỡng (3 lần đầu OK, lần thứ 4 bị chặn)
        for (int i = 0; i < 3; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Client-Id", clientId);
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // Lần tiếp theo nên bị chặn
        var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "/");
        blockedRequest.Headers.Add("X-Client-Id", clientId);
        var blockedResponse = await client.SendAsync(blockedRequest);

        // Assert
        blockedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await blockedResponse.Content.ReadAsStringAsync();
        body.Should().Contain("RATE_LIMIT_EXCEEDED");
    }
}
