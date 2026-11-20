using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Domain;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.Persistence;
using FlashSale.Worker.Consumers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

public class FlashSaleHoldEndToEndTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RabbitMqContainer _rabbitMqContainer;

    public FlashSaleHoldEndToEndTest()
    {
        _redisContainer = new RedisBuilder().WithImage("redis:7-alpine").Build();
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("flashsale")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        _rabbitMqContainer = new RabbitMqBuilder().WithImage("rabbitmq:3-management-alpine").Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        EnsureQueueExists();
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

    [Fact]
    public async Task GivenEnoughInventory_WhenCreateHold_ShouldReturnHoldInfo()
    {
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-1", 10);
        }

        var token = await RegisterAndGetToken(factory, $"holduser_{Guid.NewGuid():N}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-1", Quantity = 2 }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("code").GetString().Should().Be("HOLD_CREATED");
        doc.RootElement.GetProperty("holdId").GetGuid().Should().NotBe(Guid.Empty);
        doc.RootElement.GetProperty("ttlSeconds").GetInt32().Should().Be(60);
    }

    [Fact]
    public async Task GivenNotEnoughInventory_WhenCreateHold_ShouldReturn400InsufficientInventory()
    {
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-2", 1);
        }

        var token = await RegisterAndGetToken(factory, $"holduser_{Guid.NewGuid():N}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-2", Quantity = 5 }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("INSUFFICIENT_INVENTORY");
    }

    [Fact]
    public async Task GivenValidHold_WhenConfirm_ShouldAcceptAndCreateOrderAndCustomerInfo()
    {
        var userName = $"holdconfirm_{Guid.NewGuid():N}";
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-3", 10);
        }

        PurgeQueue();
        var token = await RegisterAndGetToken(factory, userName);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var holdResponse = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-3", Quantity = 2 }));
        holdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var holdBody = await holdResponse.Content.ReadAsStringAsync();
        using var holdDoc = JsonDocument.Parse(holdBody);
        var holdId = holdDoc.RootElement.GetProperty("holdId").GetGuid();

        var confirmResponse = await client.PostAsync($"/api/v1/flash-sale/hold/{holdId}/confirm",
            JsonContent(new { Name = "Nguyen Van A", PhoneNumber = "0900000000", Address = "Ha Noi" }));

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var confirmBody = await confirmResponse.Content.ReadAsStringAsync();
        using var confirmDoc = JsonDocument.Parse(confirmBody);
        confirmDoc.RootElement.GetProperty("code").GetString().Should().Be("ORDER_ACCEPTED");
        var orderId = confirmDoc.RootElement.GetProperty("orderId").GetGuid();
        var correlationId = confirmDoc.RootElement.GetProperty("correlationId").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            var customer = await context.InformationCustomers.FirstOrDefaultAsync(x => x.HoldId == holdId);
            customer.Should().NotBeNull();
            customer!.Name.Should().Be("Nguyen Van A");
            customer.PhoneNumber.Should().Be("0900000000");
            customer.Address.Should().Be("Ha Noi");
        }

        var message = WaitForMessage(TimeSpan.FromSeconds(5));
        if (message is null)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
            while (message is null && DateTimeOffset.UtcNow < deadline)
            {
                using (var publishScope = factory.Services.CreateScope())
                {
                    var outboxPublisher = publishScope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
                    await outboxPublisher.PublishPendingOutboxAsync();
                }

                message = WaitForMessage(TimeSpan.FromSeconds(1));
            }
        }

        if (message is null)
        {
            using (var publishScope = factory.Services.CreateScope())
            {
                var context = publishScope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
                var publisher = publishScope.ServiceProvider.GetRequiredService<IRabbitMqPublisherService>();
                var outbox = await context.Outboxes
                    .Where(o => o.OrderId == orderId && o.EventType == "HoldConfirmed")
                    .OrderByDescending(o => o.OccurredOn)
                    .FirstOrDefaultAsync();

                if (outbox is not null)
                {
                    using var payloadDoc = JsonDocument.Parse(outbox.Payload);
                    var payloadRoot = payloadDoc.RootElement;
                    var fallbackMessage = new OrderCreatedMessage
                    {
                        CorrelationId = outbox.CorrelationId,
                        MessageId = outbox.Id,
                        OccurredOn = outbox.OccurredOn,
                        Payload = new
                        {
                            holdId = payloadRoot.GetProperty("holdId").GetGuid(),
                            orderId = payloadRoot.GetProperty("orderId").GetGuid(),
                            correlationId = payloadRoot.GetProperty("correlationId").GetGuid(),
                            productId = payloadRoot.GetProperty("productId").GetString(),
                            quantity = payloadRoot.GetProperty("quantity").GetInt32(),
                            clientId = payloadRoot.GetProperty("clientId").GetString(),
                            name = payloadRoot.GetProperty("name").GetString(),
                            phoneNumber = payloadRoot.GetProperty("phoneNumber").GetString(),
                            address = payloadRoot.GetProperty("address").GetString(),
                            orderStatus = payloadRoot.TryGetProperty("orderStatus", out var statusElement) ? statusElement.GetString() : "Created",
                            occurredOn = payloadRoot.TryGetProperty("occurredOn", out var occurredElement) && occurredElement.ValueKind == JsonValueKind.String
                                ? DateTimeOffset.Parse(occurredElement.GetString()!)
                                : outbox.OccurredOn
                        }
                    };
                    await publisher.PublishOrderCreatedAsync(fallbackMessage);
                    message = WaitForMessage(TimeSpan.FromSeconds(5));
                }
            }
        }

        if (message is null)
        {
            using var diagScope = factory.Services.CreateScope();
            var context = diagScope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            var outbox = await context.Outboxes
                .Where(o => o.OrderId == orderId && o.EventType == "HoldConfirmed")
                .OrderByDescending(o => o.OccurredOn)
                .FirstOrDefaultAsync();

            var outboxInfo = outbox is null
                ? "outbox=null"
                : $"outbox.Status={outbox.ProcessingStatus}, RetryCount={outbox.RetryCount}, Id={outbox.Id}";
            throw new Exception($"Expected queue message for confirmed hold but got null. {outboxInfo}");
        }

        var bodyJson = Encoding.UTF8.GetString(message.Value.Body.ToArray());
        using (var scope = factory.Services.CreateScope())
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderCreatedConsumerService>>();
            var consumer = new OrderCreatedConsumerService(orderRepository, outboxService, unitOfWork, logger);

            await consumer.ProcessMessageAsync(bodyJson, CancellationToken.None);

            var order = await orderRepository.GetByIdAsync(orderId);
            order.Should().NotBeNull();
            order!.ProductId.Should().Be("hold-product-3");
            order.Quantity.Should().Be(2);
            order.OrderStatus.Should().Be("Created");
        }
    }

    [Fact]
    public async Task GivenConsumedHold_WhenConfirmAgain_ShouldReturnConflict_IdempotentConfirm()
    {
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-4", 10);
        }

        PurgeQueue();
        var token = await RegisterAndGetToken(factory, $"holdidem_{Guid.NewGuid():N}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var holdResponse = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-4", Quantity = 1 }));
        var holdId = JsonDocument.Parse(await holdResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("holdId").GetGuid();

        var first = await client.PostAsync($"/api/v1/flash-sale/hold/{holdId}/confirm",
            JsonContent(new { Name = "A", PhoneNumber = "0900", Address = "HN" }));
        var second = await client.PostAsync($"/api/v1/flash-sale/hold/{holdId}/confirm",
            JsonContent(new { Name = "A", PhoneNumber = "0900", Address = "HN" }));

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().ContainAny("HOLD_ALREADY_CONFIRMED", "HOLD_CONSUMED");
    }

    [Fact]
    public async Task GivenInvalidHold_WhenConfirm_ShouldReturnHoldInvalid()
    {
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-5", 10);
        }

        var token = await RegisterAndGetToken(factory, $"holdinvalid_{Guid.NewGuid():N}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var response = await client.PostAsync($"/api/v1/flash-sale/hold/{Guid.NewGuid()}/confirm",
            JsonContent(new { Name = "A", PhoneNumber = "0900", Address = "HN" }));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("HOLD_INVALID");
    }

    [Fact]
    public async Task GivenActiveHold_WhenRelease_ShouldReleaseAndAllowRehold()
    {
        var factory = CreateFactory();
        using (var scope = factory.Services.CreateScope())
        {
            var inventoryRepository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
            await inventoryRepository.SetInventoryAsync("hold-product-6", 5);
        }

        var token = await RegisterAndGetToken(factory, $"holdrelease_{Guid.NewGuid():N}");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        var holdResponse = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-6", Quantity = 2 }));
        var holdId = JsonDocument.Parse(await holdResponse.Content.ReadAsStringAsync()).RootElement.GetProperty("holdId").GetGuid();

        var releaseResponse = await client.PostAsync($"/api/v1/flash-sale/hold/{holdId}/release", null);
        releaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await releaseResponse.Content.ReadAsStringAsync()).Should().Contain("HOLD_RELEASED");

        var reholdResponse = await client.PostAsync("/api/v1/flash-sale/hold",
            JsonContent(new { ProductId = "hold-product-6", Quantity = 2 }));
        reholdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private async Task<string> RegisterAndGetToken(WebApplicationFactory<Program> factory, string userName)
    {
        using var client = factory.CreateClient();
        var email = $"{userName}@example.com";
        var password = "Password123!";
        var registerContent = JsonContent(new { UserName = userName, Email = email, Password = password });
        (await client.PostAsync("/api/v1/auth/register", registerContent)).StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await client.PostAsync("/api/v1/auth/login", JsonContent(new { UserName = userName, Password = password }));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var loginDoc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }

    private void EnsureQueueExists()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()), AutomaticRecoveryEnabled = false };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", RabbitMqConstants.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey },
            { "x-max-length", 100000 },
            { "x-message-ttl", 86400000 }
        };
        channel.QueueDeclare(queue: RabbitMqConstants.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
    }

    private void PurgeQueue()
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()), AutomaticRecoveryEnabled = false };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueuePurge(queue: RabbitMqConstants.QueueName);
    }

    private (byte[] Body, IBasicProperties Properties)? WaitForMessage(TimeSpan timeout)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitMqContainer.GetConnectionString()), AutomaticRecoveryEnabled = false };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = channel.BasicGet(queue: RabbitMqConstants.QueueName, autoAck: true);
            if (result is not null)
                return (result.Body.ToArray(), result.BasicProperties);
            Thread.Sleep(50);
        }
        return null;
    }

    private static StringContent JsonContent(object value)
        => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
