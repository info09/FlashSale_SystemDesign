using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using FlashSale.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Net;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class HealthCheckIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public HealthCheckIntegrationTest()
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

            // Dang ky IConnection RabbitMQ (singleton) cho HealthController
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<RabbitMQ.Client.IConnection>(sp =>
                {
                    var factory = new ConnectionFactory
                    {
                        Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
                        AutomaticRecoveryEnabled = false
                    };
                    return factory.CreateConnection();
                });
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
    public async Task Health_WhenAllServicesHealthy_ShouldReturn200OK()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/health");

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Health check failed with status {response.StatusCode}. Body: {body}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body2);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("PostgreSQLPrimary").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("Redis").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("checks").GetProperty("RabbitMQ").GetString().Should().Be("Healthy");
    }
}
