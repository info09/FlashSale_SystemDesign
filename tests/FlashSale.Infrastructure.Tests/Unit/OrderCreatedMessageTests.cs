using System.Text.Json;
using FluentAssertions;
using FlashSale.Domain.Entities;
using Xunit;
using FlashSale.Application.Features.Inventory.Events;

namespace FlashSale.Infrastructure.Tests.Unit;

public class OrderCreatedMessageTests
{
    [Fact]
    public void Create_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var order = new Order("product-1", 5, "client-1");
        order.UpdateStatus("Created");

        // Act
        var message = OrderCreatedMessage.Create(order, correlationId);

        // Assert
        message.Should().NotBeNull();
        message.CorrelationId.Should().Be(correlationId);
        message.MessageId.Should().NotBe(Guid.Empty);
        message.OccurredOn.Should().Be(order.CreatedOn);
        message.Payload.Should().NotBeNull();
    }

    [Fact]
    public void Create_ShouldIncludeCorrectPayload()
    {
        // Arrange
        var order = new Order("product-1", 5, "client-1");
        order.UpdateStatus("Created");
        var correlationId = Guid.NewGuid();

        // Act
        var message = OrderCreatedMessage.Create(order, correlationId);

        // Assert
        var json = message.ToJson();
        json.Should().Contain(order.Id.ToString());
        json.Should().Contain("product-1");
        json.Should().Contain("5");
        json.Should().Contain("client-1");
        json.Should().Contain("Created");
    }

    [Fact]
    public void ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        var order = new Order("product-1", 5, "client-1");
        order.UpdateStatus("Created");
        var message = OrderCreatedMessage.Create(order, Guid.NewGuid());

        // Act
        var json = message.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("orderId");
        json.Should().Contain("productId");
        json.Should().Contain("quantity");
        json.Should().Contain("clientId");
        json.Should().Contain("orderStatus");
    }
}
