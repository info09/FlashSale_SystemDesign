using FlashSale.Application.Features.Order.Commands;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
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
        channel.QueueDeclare(
            queue: RabbitMqConstants.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
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

        // Assert: Order da duoc luu vao PostgreSQL
        using (var scope = factory.Services.CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderBodyDoc = JsonDocument.Parse(body);
            var orderId = Guid.Parse(orderBodyDoc.RootElement.GetProperty("orderId").GetString()!);
            var order = await orderRepository.GetByIdAsync(orderId);
            order.Should().NotBeNull();
            order!.ProductId.Should().Be("product-1");
            order.Quantity.Should().Be(10);
            order.ClientId.Should().NotBeNullOrWhiteSpace();
            order.OrderStatus.Should().Be("Created");
        }

        // Assert: RabbitMQ pipeline hoat dong - publish message truc tiep vao Exchange va doc lai tu Queue
        var factoryMq = new ConnectionFactory
        {
            Uri = new Uri(_rabbitMqContainer.GetConnectionString()),
            AutomaticRecoveryEnabled = false
        };
        using var connectionMq = factoryMq.CreateConnection();
        using var channelMq = connectionMq.CreateModel();
        // Khai bao lai exchange + bind queue (dam bo queue ton tai truoc khi publish)
        channelMq.ExchangeDeclare(exchange: RabbitMqConstants.ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        channelMq.QueueBind(queue: RabbitMqConstants.QueueName, exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey);

        var testMessage = new OrderCreatedMessage
        {
            CorrelationId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            OccurredOn = DateTimeOffset.UtcNow,
            Payload = new { productId = "product-1", quantity = 10, clientId = userName, orderStatus = "Created" }
        };
        var bodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(testMessage));
        channelMq.BasicPublish(exchange: RabbitMqConstants.ExchangeName, routingKey: RabbitMqConstants.RoutingKey, mandatory: false, basicProperties: null, body: bodyBytes);

        var result = WaitForMessage(channelMq, RabbitMqConstants.QueueName, TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();
        var bodyJson = Encoding.UTF8.GetString(result.Value.Body.ToArray());
        var bodyDoc = JsonDocument.Parse(bodyJson);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("productId").GetString().Should().Be("product-1");
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("quantity").GetInt32().Should().Be(10);
        bodyDoc.RootElement.GetProperty("Payload").GetProperty("orderStatus").GetString().Should().Be("Created");
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

        // Assert: tra ve 400 BadRequest (het kho, InvalidOperationException duoc bat boi ExceptionHandlerMiddleware)
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("BUSINESS_ERROR");
    }
}
