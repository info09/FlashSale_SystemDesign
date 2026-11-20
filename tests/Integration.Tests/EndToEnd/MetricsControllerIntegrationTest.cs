using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class MetricsControllerIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public MetricsControllerIntegrationTest()
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
                services.AddSingleton<IRabbitMqConnectionProvider>(_ =>
                    new RabbitMqConnectionProvider(_rabbitMqContainer.GetConnectionString()));
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
    public async Task Metrics_WhenAllServicesHealthy_ShouldReturn200WithPrometheusFormat()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");

        body.Should().Contain("# HELP flash_sale_p99latency");
        body.Should().Contain("# TYPE flash_sale_p99latency gauge");
        body.Should().Contain("flash_sale_p99latency ");
        body.Should().Contain("# HELP flash_sale_errorrate");
        body.Should().Contain("# TYPE flash_sale_errorrate gauge");
        body.Should().Contain("flash_sale_errorrate ");
        body.Should().Contain("# HELP flash_sale_queuedepth");
        body.Should().Contain("flash_sale_queuedepth ");
        body.Should().Contain("# HELP flash_sale_dlqsize");
        body.Should().Contain("flash_sale_dlqsize ");
        body.Should().Contain("# HELP flash_sale_inventoryremaining");
        body.Should().Contain("flash_sale_inventoryremaining ");
    }

    [Fact]
    public async Task MetricByName_WhenExists_ShouldReturn200()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/metrics/P99Latency");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("name").GetString().Should().Be("P99Latency");
        doc.RootElement.GetProperty("unit").GetString().Should().Be("ms");
    }

    [Fact]
    public async Task MetricByName_WhenNotExists_ShouldReturn404()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/metrics/NonExistentMetric");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
