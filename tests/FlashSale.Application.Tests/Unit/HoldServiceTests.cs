using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Features.Inventory.Events;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class HoldServiceTests
{
    private readonly Mock<IHoldRepository> _holdRepository = new();
    private readonly Mock<IInformationCustomerRepository> _informationCustomerRepository = new();
    private readonly Mock<IOutboxService> _outboxService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRabbitMqPublisherService> _rabbitMqPublisherService = new();
    private readonly Mock<ILogger<HoldService>> _logger = new();
    private readonly HoldService _sut;

    public HoldServiceTests()
    {
        _sut = new HoldService(
            _holdRepository.Object,
            _informationCustomerRepository.Object,
            _outboxService.Object,
            _unitOfWork.Object,
            _rabbitMqPublisherService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenSuccess_ShouldReturnHoldInfo()
    {
        var holdId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(60);
        _holdRepository
            .Setup(x => x.CreateHoldAsync("p1", 2, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldCreateResult(HoldCreationStatus.Success, holdId, expiresAt, 60));

        var result = await _sut.CreateHoldAsync(new CreateHoldCommand("p1", 2, "c1"));

        result.HoldId.Should().Be(holdId);
        result.ExpiresAt.Should().Be(expiresAt);
        result.TtlSeconds.Should().Be(60);
    }

    [Fact]
    public async Task CreateHoldAsync_WhenInsufficientInventory_ShouldThrowInsufficientInventoryException()
    {
        _holdRepository
            .Setup(x => x.CreateHoldAsync("p1", 2, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldCreateResult(HoldCreationStatus.InsufficientInventory, AvailableQuantity: 1));

        var act = () => _sut.CreateHoldAsync(new CreateHoldCommand("p1", 2, "c1"));

        await act.Should().ThrowAsync<InsufficientInventoryException>();
    }

    [Fact]
    public async Task CreateHoldAsync_WhenActiveHoldExists_ShouldThrowActiveHoldAlreadyExistsException()
    {
        var existingHoldId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.CreateHoldAsync("p1", 2, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldCreateResult(HoldCreationStatus.ActiveHoldAlreadyExists, ExistingHoldId: existingHoldId));

        var act = () => _sut.CreateHoldAsync(new CreateHoldCommand("p1", 2, "c1"));

        (await act.Should().ThrowAsync<ActiveHoldAlreadyExistsException>())
            .Which.ExistingHoldId.Should().Be(existingHoldId);
    }

    [Theory]
    [InlineData("", 1, "c1")]
    [InlineData("p1", 0, "c1")]
    [InlineData("p1", 1, "")]
    public async Task CreateHoldAsync_InvalidInput_ShouldThrowArgumentException(string productId, int quantity, string clientId)
    {
        var act = () => _sut.CreateHoldAsync(new CreateHoldCommand(productId, quantity, clientId));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenSuccess_ShouldConsumeHoldPersistCustomerAddOutboxAndPublish()
    {
        var holdId = Guid.NewGuid();
        var hold = new Hold(holdId, "p1", 2, "c1", DateTimeOffset.UtcNow.AddSeconds(30));
        var outbox = new Outbox(Guid.NewGuid(), "HoldConfirmed", "{}", Guid.NewGuid());

        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.Success, hold));
        _outboxService
            .Setup(x => x.AddOutboxAsync(It.IsAny<Guid>(), "HoldConfirmed", It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outbox);

        var result = await _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        result.HoldId.Should().Be(holdId);
        result.ProductId.Should().Be("p1");
        result.Quantity.Should().Be(2);
        result.ClientId.Should().Be("c1");
        result.OrderId.Should().NotBe(Guid.Empty);
        result.CorrelationId.Should().NotBe(Guid.Empty);

        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _informationCustomerRepository.Verify(x => x.AddAsync(
            It.Is<InformationCustomer>(i => i.HoldId == holdId && i.Name == "Name" && i.PhoneNumber == "0900" && i.Address == "Addr"),
            It.IsAny<CancellationToken>()), Times.Once);
        _outboxService.Verify(x => x.AddOutboxAsync(
            result.OrderId,
            "HoldConfirmed",
            It.Is<string>(p => p.Contains(holdId.ToString()) && p.Contains("Name") && p.Contains("0900") && p.Contains("Addr")),
            result.CorrelationId,
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _rabbitMqPublisherService.Verify(x => x.PublishOrderCreatedAsync(
            It.Is<OrderCreatedMessage>(m => m.CorrelationId == result.CorrelationId && m.MessageId == outbox.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        outbox.ProcessingStatus.Should().Be("Published");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenImmediatePublishFails_ShouldKeepOutboxPendingForRetry()
    {
        var holdId = Guid.NewGuid();
        var hold = new Hold(holdId, "p1", 2, "c1", DateTimeOffset.UtcNow.AddSeconds(30));
        var outbox = new Outbox(Guid.NewGuid(), "HoldConfirmed", "{}", Guid.NewGuid());

        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.Success, hold));
        _outboxService
            .Setup(x => x.AddOutboxAsync(It.IsAny<Guid>(), "HoldConfirmed", It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outbox);
        _rabbitMqPublisherService
            .Setup(x => x.PublishOrderCreatedAsync(It.IsAny<OrderCreatedMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));

        var result = await _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        result.HoldId.Should().Be(holdId);
        _unitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        outbox.ProcessingStatus.Should().Be("Pending");
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenExpired_ShouldThrowHoldExpiredException()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.Expired));

        var act = () => _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        await act.Should().ThrowAsync<HoldExpiredException>();
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenNotFoundOrInvalid_ShouldThrowHoldInvalidException()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.NotFound));

        var act = () => _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        await act.Should().ThrowAsync<HoldInvalidException>();
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenConsumed_ShouldThrowHoldAlreadyConfirmedException()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.Consumed));

        var act = () => _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        await act.Should().ThrowAsync<HoldAlreadyConfirmedException>();
    }

    [Fact]
    public async Task ConfirmHoldAsync_WhenNotOwned_ShouldThrowHoldNotOwnedException()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ConsumeHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldConsumeResult(HoldConsumeStatus.NotOwned));

        var act = () => _sut.ConfirmHoldAsync(new ConfirmHoldCommand(holdId, "c1", "Name", "0900", "Addr"));

        await act.Should().ThrowAsync<HoldNotOwnedException>();
    }

    [Theory]
    [InlineData("", "0900", "Addr")]
    [InlineData("Name", "", "Addr")]
    [InlineData("Name", "0900", "")]
    public async Task ConfirmHoldAsync_WhenCustomerInfoMissing_ShouldThrowArgumentException(string name, string phone, string address)
    {
        var act = () => _sut.ConfirmHoldAsync(new ConfirmHoldCommand(Guid.NewGuid(), "c1", name, phone, address));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenReleased_ShouldSucceed()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ReleaseHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldReleaseResult(HoldReleaseStatus.Released));

        var result = await _sut.ReleaseHoldAsync(new ReleaseHoldCommand(holdId, "c1"));

        result.Released.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenNotFound_ShouldThrowHoldInvalidException()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ReleaseHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldReleaseResult(HoldReleaseStatus.NotFound));

        var act = () => _sut.ReleaseHoldAsync(new ReleaseHoldCommand(holdId, "c1"));

        await act.Should().ThrowAsync<HoldInvalidException>();
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenExpiredReleased_ShouldSucceedIdempotent()
    {
        var holdId = Guid.NewGuid();
        _holdRepository
            .Setup(x => x.ReleaseHoldAsync(holdId, "c1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HoldReleaseResult(HoldReleaseStatus.ExpiredReleased));

        var result = await _sut.ReleaseHoldAsync(new ReleaseHoldCommand(holdId, "c1"));

        result.Released.Should().BeTrue();
    }
}
