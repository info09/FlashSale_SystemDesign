using FluentAssertions;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _orderService = new OrderService(
            _orderRepositoryMock.Object,
            _inventoryRepositoryMock.Object,
            _outboxServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenEnoughInventory_ShouldCreateOrderAndSaveOutboxInOneTransaction()
    {
        // Arrange
        var productId = "product-1";
        var quantity = 10;
        var clientId = "client-1";
        var remainingQuantity = 90;

        _inventoryRepositoryMock
            .Setup(x => x.DeductInventoryAsync(productId, quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingQuantity);

        // Act
        var order = await _orderService.CreateOrderAsync(productId, quantity, clientId);

        // Assert
        order.Should().NotBeNull();
        order.ProductId.Should().Be(productId);
        order.Quantity.Should().Be(quantity);
        order.ClientId.Should().Be(clientId);
        order.OrderStatus.Should().Be("Created");

        // Order va Outbox duoc luu trong cung 1 transaction
        _orderRepositoryMock.Verify(x => x.AddAsync(It.Is<Order>(o =>
            o.ProductId == productId &&
            o.Quantity == quantity &&
            o.ClientId == clientId), It.IsAny<CancellationToken>()), Times.Once);

        _outboxServiceMock.Verify(x => x.AddOutboxAsync(
            order.Id,
            "OrderCreated",
            It.Is<string>(p => p.Contains("\"productId\":\"product-1\"")),
            order.Id,
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenNotEnoughInventory_ShouldThrowAndNotCreateOrder()
    {
        // Arrange
        var productId = "product-2";
        var quantity = 100;
        var clientId = "client-2";

        _inventoryRepositoryMock
            .Setup(x => x.DeductInventoryAsync(productId, quantity, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        // Act & Assert
        var act = () => _orderService.CreateOrderAsync(productId, quantity, clientId);
        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*đã hết hàng*");

        // Khong co Order nao duoc them, khong commit transaction
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _outboxServiceMock.Verify(x => x.AddOutboxAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrderAsync_WhenProductIdEmpty_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _orderService.CreateOrderAsync(string.Empty, 10, "client-1");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*ProductId không được để trống*");
    }

    [Fact]
    public async Task CreateOrderAsync_WhenQuantityZero_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _orderService.CreateOrderAsync("product-1", 0, "client-1");

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*Quantity phải lớn hơn 0*");
    }

    [Fact]
    public async Task CreateOrderAsync_WhenClientIdEmpty_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _orderService.CreateOrderAsync("product-1", 10, string.Empty);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*ClientId không được để trống*");
    }
}
