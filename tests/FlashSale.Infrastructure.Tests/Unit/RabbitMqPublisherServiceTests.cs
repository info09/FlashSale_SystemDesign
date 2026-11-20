using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Domain;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.RabbitMQ;
using FluentAssertions;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class RabbitMqPublisherServiceTests
{
    private readonly Mock<IConnection> _connectionMock;
    private readonly Mock<IModel> _channelMock;

    public RabbitMqPublisherServiceTests()
    {
        _connectionMock = new Mock<IConnection>();
        _channelMock = new Mock<IModel>();

        _connectionMock
            .Setup(c => c.CreateModel())
            .Returns(_channelMock.Object);

        _channelMock
            .Setup(c => c.IsOpen)
            .Returns(true);
    }

    [Fact]
    public void Constructor_WithConnection_ShouldNotCreateChannelEagerly()
    {
        var service = new RabbitMqPublisherService(_connectionMock.Object);

        service.Should().NotBeNull();
        _connectionMock.Verify(c => c.CreateModel(), Times.Never);
    }

    [Fact]
    public async Task PublishOrderCreatedAsync_ShouldDeclareTopologyAndPublishMessageToPipeline()
    {
        var service = new RabbitMqPublisherService(_connectionMock.Object);
        var message = OrderCreatedMessage.Create(
            new Order("product-1", 5, "client-1"),
            Guid.NewGuid());

        var propertiesMock = new Mock<IBasicProperties>();
        _channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);

        await service.PublishOrderCreatedAsync(message);

        _connectionMock.Verify(c => c.CreateModel(), Times.Once);
        _channelMock.Verify(c => c.ExchangeDeclare(
            RabbitMqConstants.ExchangeName,
            ExchangeType.Direct,
            true,
            false,
            null), Times.Once);
        _channelMock.Verify(c => c.BasicPublish(
            RabbitMqConstants.ExchangeName,
            RabbitMqConstants.RoutingKey,
            true,
            It.IsAny<IBasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(b => b.Length > 0)), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishToSpecifiedExchangeAndRoutingKey()
    {
        var service = new RabbitMqPublisherService(_connectionMock.Object);
        var propertiesMock = new Mock<IBasicProperties>();
        _channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);

        await service.PublishAsync("ex", "rk", "{\"x\":1}", Guid.NewGuid(), Guid.NewGuid());

        _channelMock.Verify(c => c.BasicPublish(
            "ex",
            "rk",
            true,
            It.IsAny<IBasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(b => b.Length > 0)), Times.Once);
    }

    [Fact]
    public async Task Dispose_AfterPublish_ShouldCloseChannelAndConnection()
    {
        var service = new RabbitMqPublisherService(_connectionMock.Object);
        var message = OrderCreatedMessage.Create(new Order("product-1", 5, "client-1"), Guid.NewGuid());
        _channelMock.Setup(c => c.CreateBasicProperties()).Returns(Mock.Of<IBasicProperties>());

        await service.PublishOrderCreatedAsync(message);
        service.Dispose();

        _channelMock.Verify(c => c.Close(), Times.AtLeastOnce);
        _channelMock.Verify(c => c.Dispose(), Times.AtLeastOnce);
        _connectionMock.Verify(c => c.Close(), Times.AtLeastOnce);
        _connectionMock.Verify(c => c.Dispose(), Times.AtLeastOnce);
    }
}
