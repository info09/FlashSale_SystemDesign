using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Infrastructure.Persistence;
using FlashSale.Worker.Consumers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Net;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class ReconciliationEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public ReconciliationEndToEndTest()
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
        EnsureQueueExists();
    }

    private void EnsureQueueExists()
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(exchange: RabbitMqConstants.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        channel.ExchangeDeclare(exchange: RabbitMqConstants.DeadLetterExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey },
            { "x-max-length", 100000 },
            { "x-message-ttl", 86400000 }
        };
        channel.QueueDeclare(
            queue: RabbitMqConstants.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);
        channel.QueueBind(queue: RabbitMqConstants.QueueName, exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey);
        channel.QueueDeclare(queue: RabbitMqConstants.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queue: RabbitMqConstants.DeadLetterQueueName, exchange: RabbitMqConstants.DeadLetterExchangeName, routingKey: RabbitMqConstants.DeadLetterRoutingKey);
    }

    public async Task DisposeAsync()
    {
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.StopAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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
        });
    }

    private static string BuildMessageJson(Guid orderId, string productId, int quantity, string clientId, Guid correlationId, Guid messageId)
    {
        var occurredOn = DateTimeOffset.UtcNow;
        var message = new
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            OccurredOn = occurredOn,
            Payload = new { orderId, productId, quantity, clientId, orderStatus = "Created", createdOn = occurredOn }
        };
        return JsonSerializer.Serialize(message);
    }

    private void PurgeQueue()
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueuePurge(queue: RabbitMqConstants.QueueName);
    }

    [Fact]
    public async Task AllOrdersHaveOutbox_WhenReconcile_ShouldBeConsistent()
    {
        // Arrange
        var userName = $"reconuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<FlashSale.Domain.Interfaces.IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("product-1", 100);
        }

        PurgeQueue();

        var token = await RegisterAndGetToken(factory, userName, $"{userName}@example.com", "Password123!");

        using var client = factory.CreateClient();
        var request = new { ProductId = "product-1", Quantity = 5 };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act: API tra ve 202 Accepted
        var response = await client.PostAsync("/api/v1/flash-sale/buy", content);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Publish OrderCreatedMessage vao Queue
        var factoryMq = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        using var connectionMq = factoryMq.CreateConnection();
        using var channelMq = connectionMq.CreateModel();
        channelMq.ExchangeDeclare(exchange: RabbitMqConstants.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        channelMq.QueueBind(queue: RabbitMqConstants.QueueName, exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey);

        var correlationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var testMessage = new OrderCreatedMessage
        {
            CorrelationId = correlationId,
            MessageId = messageId,
            OccurredOn = DateTimeOffset.UtcNow,
            Payload = new { orderId = Guid.NewGuid(), productId = "product-1", quantity = 5, clientId = userName, orderStatus = "Created", createdOn = DateTimeOffset.UtcNow }
        };
        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
        channelMq.BasicPublish(exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey, mandatory: false, basicProperties: null, body: bodyBytes);

        var result = WaitForMessage(channelMq, RabbitMqConstants.QueueName, TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();
        var bodyJson = Encoding.UTF8.GetString(result.Value.Body.ToArray());
        var bodyDoc = JsonDocument.Parse(bodyJson);
        var orderIdFromMessage = Guid.Parse(bodyDoc.RootElement.GetProperty("Payload").GetProperty("orderId").GetString()!);
        var correlationIdFromMessage = Guid.Parse(bodyDoc.RootElement.GetProperty("CorrelationId").GetString()!);

        // Async Worker xu ly message: luu Order + Outbox trong cung 1 transaction
        using (var scope = factory.Services.CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<FlashSale.Domain.Interfaces.IOrderRepository>();
            var outboxService = scope.ServiceProvider.GetRequiredService<FlashSale.Application.Interfaces.IOutboxService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<FlashSale.Domain.Interfaces.IUnitOfWork>();
            var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OrderCreatedConsumerService>>();
            var consumer = new OrderCreatedConsumerService(orderRepository, outboxService, unitOfWork, logger);
            await consumer.ProcessMessageAsync(bodyJson, CancellationToken.None);
        }

        // Reconciliation: tat ca don 202 Accepted (co Outbox) phai co Order trong PostgreSQLPrimary
        using (var scope = factory.Services.CreateScope())
        {
            var reconciliationService = scope.ServiceProvider.GetRequiredService<FlashSale.Application.Interfaces.IReconciliationService>();
            var reconResult = await reconciliationService.ReconcileAsync();

            reconResult.TotalChecked.Should().BeGreaterThanOrEqualTo(1);
            // Don vua nhan 202 Accepted (correlationIdFromMessage) phai co Order trong PostgreSQLPrimary (NFR-06)
            reconResult.MissingCorrelationIds.Should().NotContain(correlationIdFromMessage, $"Don {correlationIdFromMessage} da nhan 202 Accepted nhung bi thieu Order");
        }
    }

    [Fact]
    public async Task OutboxWithoutOrder_WhenReconcile_ShouldReportMissing()
    {
        // Arrange
        var factory = CreateFactory();
        var orderId = Guid.NewGuid(); // Order chua ton tai trong PostgreSQL
        var correlationId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            var outbox = new Outbox(
                Guid.NewGuid(), orderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, correlationId, "Pending", 0, null);
            context.Outboxes.Add(outbox);
            await context.SaveChangesAsync();
        }

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var reconciliationService = scope.ServiceProvider.GetRequiredService<FlashSale.Application.Interfaces.IReconciliationService>();
            var reconResult = await reconciliationService.ReconcileAsync();

            // Assert: co it nhat 1 don bi mat (Outbox co nhung khong co Order)
            reconResult.TotalChecked.Should().BeGreaterThanOrEqualTo(1);
            reconResult.MissingOrderCount.Should().BeGreaterThanOrEqualTo(1);
            reconResult.IsConsistent.Should().BeFalse();
            reconResult.MissingCorrelationIds.Should().Contain(correlationId);
        }
    }

    private static (byte[] Body, RabbitMQ.Client.IBasicProperties Properties)? WaitForMessage(RabbitMQ.Client.IModel channel, string queueName, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = channel.BasicGet(queue: queueName, autoAck: true);
            if (result is not null)
                return (result.Body.ToArray(), result.BasicProperties);
            Thread.Sleep(50);
        }
        return null;
    }

    private async Task<string> RegisterAndGetToken(WebApplicationFactory<Program> factory, string userName, string email, string password)
    {
        using var client = factory.CreateClient();
        var registerRequest = new { UserName = userName, Email = email, Password = password };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var loginRequest = new { UserName = userName, Password = password };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }
}

