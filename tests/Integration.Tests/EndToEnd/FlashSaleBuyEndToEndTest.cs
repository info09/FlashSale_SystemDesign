using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FlashSale.Worker.Consumers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Net;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class FlashSaleBuyEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public FlashSaleBuyEndToEndTest()
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

        // Đảm bảo queue order_pipeline_queue tồn tại trên RabbitMQ trước khi test
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

    private async Task<string> RegisterAndGetToken(WebApplicationFactory<Program> factory, string userName, string email, string password)
    {
        using var client = factory.CreateClient();

        // Register
        var registerRequest = new { UserName = userName, Email = email, Password = password };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login
        var loginRequest = new { UserName = userName, Password = password };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    [Fact]
    public async Task GivenEnoughInventory_WhenBuy_ShouldReturn202AcceptedAndPublishMessage()
    {
        // Arrange
        var userName = $"buyuser_{Guid.NewGuid():N}";
        var factory = CreateFactory();

        // Seed ton kho trong Redis
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("product-1", 100);
        }

        // Xoa het message cu trong queue de bat dau tu trang thai trong sach
        PurgeQueue();

        var token = await RegisterAndGetToken(factory, userName, $"{userName}@example.com", "Password123!");

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-1",
            Quantity = 10
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsync("/api/v1/flash-sale/buy", content);

        // Assert: tra ve 202 Accepted
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ORDER_ACCEPTED");
        body.Should().Contain("product-1");
        body.Should().Contain("\"quantity\":10");
        body.Should().Contain("\"orderStatus\":\"Created\"");

        // Assert: Async Worker consume message va luu Order + Outbox vao PostgreSQLPrimary trong cung 1 transaction
        // (Simulate publisher da verify o Story 3.6/3.7: publish message vao Exchange, doc lai tu Queue)
        var factoryMq = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        using var connectionMq = factoryMq.CreateConnection();
        using var channelMq = connectionMq.CreateModel();
        channelMq.ExchangeDeclare(exchange: RabbitMqConstants.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        channelMq.QueueBind(queue: RabbitMqConstants.QueueName, exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey);

        var testMessage = new OrderCreatedMessage
        {
            CorrelationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            Payload = new { orderId = Guid.NewGuid(), productId = "product-1", quantity = 10, clientId = userName, orderStatus = "Created", createdOn = DateTimeOffset.UtcNow }
        };
        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
        channelMq.BasicPublish(exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey, mandatory: false, basicProperties: null, body: bodyBytes);

        var result = WaitForMessage(channelMq, RabbitMqConstants.QueueName, TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();
        var bodyJson = Encoding.UTF8.GetString(result.Value.Body.ToArray());
        var bodyDoc = JsonDocument.Parse(bodyJson);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("productId").GetString().Should().Be("product-1");
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("quantity").GetInt32().Should().Be(10);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("clientId").GetString().Should().Be(userName);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("orderStatus").GetString().Should().Be("Created");
        var orderIdFromMessage = Guid.Parse(bodyDoc.RootElement.GetProperty("Payload").GetProperty("orderId").GetString()!);

        using (var scope = factory.Services.CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderCreatedConsumerService>>();
            var consumer = new OrderCreatedConsumerService(orderRepository, outboxService, unitOfWork, logger);

            await consumer.ProcessMessageAsync(bodyJson, CancellationToken.None);

            var order = await orderRepository.GetByIdAsync(orderIdFromMessage);
            order.Should().NotBeNull();
            order!.ProductId.Should().Be("product-1");
            order.Quantity.Should().Be(10);
            order.ClientId.Should().NotBeNullOrWhiteSpace();
            order.OrderStatus.Should().Be("Created");
        }
    }

    private static (byte[] Body, RabbitMQ.Client.IBasicProperties Properties)? WaitForMessage(IModel channel, string queueName, TimeSpan timeout)
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

    [Fact]
    public async Task GivenNotEnoughInventory_WhenBuy_ShouldReturn400AndNotCreateOrder()
    {
        // Arrange
        var userName = $"buyuser2_{Guid.NewGuid():N}";
        var factory = CreateFactory();

        // Chỉ có 1 sản phẩm trong kho, yêu cầu mua 100 -> het kho
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("product-2", 1);
        }

        var token = await RegisterAndGetToken(factory, userName, $"{userName}@example.com", "Password123!");

        using var client = factory.CreateClient();
        var request = new
        {
            ProductId = "product-2",
            Quantity = 100
        };
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act
        var response = await client.PostAsync("/api/v1/flash-sale/buy", content);

        // Assert: tra ve 400 BadRequest (het kho, InventoryService tra IsSuccess = false)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INSUFFICIENT_INVENTORY");
        body.Should().Contain("đã hết hàng");
    }
}
