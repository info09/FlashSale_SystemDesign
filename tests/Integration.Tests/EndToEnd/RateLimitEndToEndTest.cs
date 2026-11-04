using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
        // Arrange - Tạo factory mới với connection string thực tế từ Testcontainers
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("RATE_LIMIT_RPS", "100"),
                    new KeyValuePair<string, string>("RATE_LIMIT_WINDOW_SECONDS", "60")
                });
            });
        });

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
        // Arrange - Tạo factory mới với rate limit thấp (3 requests / 60s)
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("RATE_LIMIT_RPS", "3"),
                    new KeyValuePair<string, string>("RATE_LIMIT_WINDOW_SECONDS", "60")
                });
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var clientId = "e2e-client-2";

        // Act: Gọi liên tục cho đến khi nhận 429 (tối đa 10 lần để tránh vô hạn)
        int successCount = 0;
        HttpStatusCode lastStatus = default;
        string lastBody = "";
        for (int i = 0; i < 10; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("X-Client-Id", clientId);
            var response = await client.SendAsync(request);
            lastStatus = response.StatusCode;
            lastBody = await response.Content.ReadAsStringAsync();
            if (lastStatus == HttpStatusCode.OK)
            {
                successCount++;
            }
            else
            {
                break; // Đã bị chặn, dừng vòng lặp
            }
        }

        // Assert: Có ít nhất 1 lần OK, cuối cùng bị chặn 429
        successCount.Should().BeGreaterThan(0);
        lastStatus.Should().Be(HttpStatusCode.TooManyRequests);
        lastBody.Should().Contain("RATE_LIMIT_EXCEEDED");
    }
}
