using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Worker.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class OutboxPublisherWorkerServiceTests
{
    private readonly Mock<IOutboxPublisher> _publisherMock;
    private readonly ILogger<OutboxPublisherWorkerService> _logger;

    public OutboxPublisherWorkerServiceTests()
    {
        _publisherMock = new Mock<IOutboxPublisher>();
        _logger = Mock.Of<ILogger<OutboxPublisherWorkerService>>();
    }

    [Fact]
    public async Task PublishOnceAsync_WithPublisher_ShouldCallPublishPendingOutboxAsync()
    {
        // Arrange
        _publisherMock
            .Setup(p => p.PublishPendingOutboxAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await OutboxPublisherWorkerService.PublishOnceAsync(_logger, _publisherMock.Object, CancellationToken.None);

        // Assert
        _publisherMock.Verify(p => p.PublishPendingOutboxAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishOnceAsync_WhenPublisherThrows_ShouldPropagateException()
    {
        // Arrange
        _publisherMock
            .Setup(p => p.PublishPendingOutboxAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ error"));

        // Act & Assert
        var act = () => OutboxPublisherWorkerService.PublishOnceAsync(_logger, _publisherMock.Object, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
