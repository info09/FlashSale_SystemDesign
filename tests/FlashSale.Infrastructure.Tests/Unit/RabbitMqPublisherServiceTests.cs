using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using FlashSale.Application.Features.Inventory.Events;

namespace FlashSale.Infrastructure.Tests.Unit;

public class RabbitMqPublisherServiceTests
{
    private readonly Mock<IConnection> _connectionMock;
    private readonly Mock<IModel> _channelMock;
    private readonly RabbitMqPublisherService _service;

    public RabbitMqPublisherServiceTests()
    {
        _connectionMock = new Mock<IConnection>();
        _channelMock = new Mock<IModel>();

        _connectionMock
            .Setup(c => c.CreateModel())
            .Returns(_channelMock.Object);

        _service = new RabbitMqPublisherService(_connectionMock.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateConnectionAndChannel()
    {
        // Arrange & Act
        var service = new RabbitMqPublisherService(_connectionMock.Object);

        // Assert
        service.Should().NotBeNull();
        _connectionMock.Verify(c => c.CreateModel(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_ShouldPublishMessageToQueue()
    {
        // Arrange
        var message = OrderCreatedMessage.Create(
            new FlashSale.Domain.Entities.Order("product-1", 5, "client-1"),
            Guid.NewGuid());

        var propertiesMock = new Mock<IBasicProperties>();
        _channelMock
            .Setup(c => c.IsOpen)
            .Returns(true);
        _channelMock
            .Setup(c => c.CreateBasicProperties())
            .Returns(propertiesMock.Object);

        // Act
        await _service.PublishOrderCreatedAsync(message);

        // Assert
        _channelMock.Verify(c => c.BasicPublish(
            It.Is<string>(e => e == RabbitMqConstants.ExchangeName),
            It.Is<string>(r => r == RabbitMqConstants.RoutingKey),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(b => b.Length > 0)
        ), Times.AtLeastOnce);
    }

    [Fact]
    public void Dispose_ShouldCloseConnectionAndChannel()
    {
        // Arrange
        var service = new RabbitMqPublisherService(_connectionMock.Object);

        // Act
        service.Dispose();

        // Assert
        _channelMock.Verify(c => c.Close(), Times.AtLeastOnce);
        _channelMock.Verify(c => c.Dispose(), Times.AtLeastOnce);
        _connectionMock.Verify(c => c.Close(), Times.AtLeastOnce);
        _connectionMock.Verify(c => c.Dispose(), Times.AtLeastOnce);
    }
}
