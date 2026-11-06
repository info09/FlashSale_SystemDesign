using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class OutboxPurgeServiceTests
{
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly OutboxPurgeService _purgeService;

    public OutboxPurgeServiceTests()
    {
        _outboxRepositoryMock = new Mock<IOutboxRepository>();

        var options = Options.Create(new OutboxPurgeOptions { PurgeDays = 30 });
        _purgeService = new OutboxPurgeService(_outboxRepositoryMock.Object, options);
    }

    [Fact]
    public async Task PurgeProcessedOutboxesAsync_ShouldCallRepositoryWithOlderThanPurgeDays()
    {
        // Arrange
        var expectedDeleted = 5;
        var expectedOlderThan = DateTimeOffset.UtcNow.AddDays(-30);

        _outboxRepositoryMock
            .Setup(x => x.PurgeProcessedOutboxesOlderThanAsync(It.Is<DateTimeOffset>(d => d <= expectedOlderThan.AddSeconds(1) && d >= expectedOlderThan.AddSeconds(-1)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDeleted);

        // Act
        var deleted = await _purgeService.PurgeProcessedOutboxesAsync();

        // Assert
        deleted.Should().Be(expectedDeleted);
        _outboxRepositoryMock.Verify(x => x.PurgeProcessedOutboxesOlderThanAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PurgeProcessedOutboxesAsync_ShouldUseCustomPurgeDaysFromOptions()
    {
        // Arrange
        var options = Options.Create(new OutboxPurgeOptions { PurgeDays = 7 });
        var purgeService = new OutboxPurgeService(_outboxRepositoryMock.Object, options);

        var expectedOlderThan = DateTimeOffset.UtcNow.AddDays(-7);
        _outboxRepositoryMock
            .Setup(x => x.PurgeProcessedOutboxesOlderThanAsync(It.Is<DateTimeOffset>(d => d <= expectedOlderThan.AddSeconds(1) && d >= expectedOlderThan.AddSeconds(-1)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var deleted = await purgeService.PurgeProcessedOutboxesAsync();

        // Assert
        deleted.Should().Be(3);
    }

    [Fact]
    public async Task PurgeProcessedOutboxesAsync_WhenRepositoryReturnsZero_ShouldReturnZero()
    {
        // Arrange
        _outboxRepositoryMock
            .Setup(x => x.PurgeProcessedOutboxesOlderThanAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var deleted = await _purgeService.PurgeProcessedOutboxesAsync();

        // Assert
        deleted.Should().Be(0);
    }
}
