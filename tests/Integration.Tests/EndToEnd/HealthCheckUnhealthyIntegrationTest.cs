using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class HealthCheckUnhealthyIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public HealthCheckUnhealthyIntegrationTest()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("flashsale")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string>("REDIS_CONNECTION_STRING", _redisContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("POSTGRESQL_PRIMARY_CONNECTION_STRING", _postgresContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("RABBITMQ_CONNECTION_STRING", _rabbitMqContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("JWT_SECRET", "test_jwt_secret_key_at_least_32_characters_long_for_hmac_sha256"),
                    new KeyValuePair<string, string>("JWT_ISSUER", "FlashSale.Api"),
                    new KeyValuePair<string, string>("JWT_AUDIENCE", "FlashSale.Client"),
                    new KeyValuePair<string, string>("JWT_EXPIRATION_MINUTES", "60"),
                ]);
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnection>(sp =>
                {
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
                        AutomaticRecoveryEnabled = false
                    };
                    return factory.CreateConnection();
                });

                // Simulate Redis dependency failure for health check
                var redisDbMock = new Mock<IDatabase>();
                redisDbMock
                    .Setup(x => x.PingAsync(It.IsAny<CommandFlags>()))
                    .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable for test"));
                services.RemoveAll<IDatabase>();
                services.AddSingleton(redisDbMock.Object);
            });
        });
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.StopAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    [Fact]
    public async Task Health_WhenRedisDown_ShouldReturn503ServiceUnavailable()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Unhealthy");
        doc.RootElement.GetProperty("checks").GetProperty("Redis").GetString().Should().Be("Unhealthy");
    }
}
