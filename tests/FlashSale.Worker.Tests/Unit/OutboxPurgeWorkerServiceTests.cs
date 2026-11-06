using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Worker.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FlashSale.Worker.Tests.Unit;

public class OutboxPurgeWorkerServiceTests
{
    private readonly Mock<IOutboxPurgeService> _purgeServiceMock;
    private readonly ILogger<OutboxPurgeWorkerService> _logger;

    public OutboxPurgeWorkerServiceTests()
    {
        _purgeServiceMock = new Mock<IOutboxPurgeService>();
        _logger = Mock.Of<ILogger<OutboxPurgeWorkerService>>();
    }

    [Fact]
    public async Task PurgeOnceAsync_WithPurgeService_ShouldCallPurgeProcessedOutboxesAsync()
    {
        // Arrange
        _purgeServiceMock
            .Setup(p => p.PurgeProcessedOutboxesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        await OutboxPurgeWorkerService.PurgeOnceAsync(_logger, _purgeServiceMock.Object, CancellationToken.None);

        // Assert
        _purgeServiceMock.Verify(p => p.PurgeProcessedOutboxesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeOnceAsync_WhenPurgeServiceReturnsZero_ShouldStillCallOnce()
    {
        // Arrange
        _purgeServiceMock
            .Setup(p => p.PurgeProcessedOutboxesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await OutboxPurgeWorkerService.PurgeOnceAsync(_logger, _purgeServiceMock.Object, CancellationToken.None);

        // Assert
        _purgeServiceMock.Verify(p => p.PurgeProcessedOutboxesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeOnceAsync_WhenPurgeServiceThrows_ShouldPropagateException()
    {
        // Arrange
        _purgeServiceMock
            .Setup(p => p.PurgeProcessedOutboxesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act & Assert
        var act = () => OutboxPurgeWorkerService.PurgeOnceAsync(_logger, _purgeServiceMock.Object, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
