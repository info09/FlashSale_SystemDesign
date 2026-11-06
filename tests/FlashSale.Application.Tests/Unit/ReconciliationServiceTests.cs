using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class ReconciliationServiceTests
{
    private readonly Mock<IReconciliationRepository> _reconciliationRepositoryMock;
    private readonly ReconciliationService _reconciliationService;

    public ReconciliationServiceTests()
    {
        _reconciliationRepositoryMock = new Mock<IReconciliationRepository>();
        _reconciliationService = new ReconciliationService(_reconciliationRepositoryMock.Object);
    }

    [Fact]
    public async Task ReconcileAsync_WhenAllOutboxesHaveMatchingOrders_ShouldReturnConsistent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var outboxes = new List<Outbox>
        {
            new Outbox(Guid.NewGuid(), orderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, correlationId, "Published", 0, DateTimeOffset.UtcNow)
        };
        var orderIds = new List<Guid> { orderId };

        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOrderIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderIds);
        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOutboxesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(outboxes);

        // Act
        var result = await _reconciliationService.ReconcileAsync();

        // Assert
        result.TotalChecked.Should().Be(1);
        result.MatchedCount.Should().Be(1);
        result.MissingOrderCount.Should().Be(0);
        result.IsConsistent.Should().BeTrue();
        result.MissingCorrelationIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_WhenSomeOutboxesMissingOrders_ShouldReturnMissing()
    {
        // Arrange
        var missingOrderId = Guid.NewGuid();
        var missingCorrelationId = Guid.NewGuid();
        var matchedOrderId = Guid.NewGuid();
        var matchedCorrelationId = Guid.NewGuid();
        var outboxes = new List<Outbox>
        {
            new Outbox(Guid.NewGuid(), missingOrderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, missingCorrelationId, "Pending", 0, null),
            new Outbox(Guid.NewGuid(), matchedOrderId, "OrderCreated", "{}", DateTimeOffset.UtcNow, matchedCorrelationId, "Published", 0, DateTimeOffset.UtcNow)
        };
        var orderIds = new List<Guid> { matchedOrderId };

        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOrderIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderIds);
        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOutboxesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(outboxes);

        // Act
        var result = await _reconciliationService.ReconcileAsync();

        // Assert
        result.TotalChecked.Should().Be(2);
        result.MatchedCount.Should().Be(1);
        result.MissingOrderCount.Should().Be(1);
        result.IsConsistent.Should().BeFalse();
        result.MissingCorrelationIds.Should().Contain(missingCorrelationId);
        result.MissingCorrelationIds.Should().NotContain(matchedCorrelationId);
    }

    [Fact]
    public async Task ReconcileAsync_WhenNoOutboxes_ShouldReturnEmptyConsistentResult()
    {
        // Arrange
        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOrderIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        _reconciliationRepositoryMock
            .Setup(x => x.GetAllOutboxesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Outbox>());

        // Act
        var result = await _reconciliationService.ReconcileAsync();

        // Assert
        result.TotalChecked.Should().Be(0);
        result.MatchedCount.Should().Be(0);
        result.MissingOrderCount.Should().Be(0);
        result.IsConsistent.Should().BeTrue();
    }
}
