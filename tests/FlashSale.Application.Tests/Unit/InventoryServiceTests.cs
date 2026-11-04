using FluentAssertions;
using FlashSale.Application.Features.Inventory.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using Moq;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class InventoryServiceTests
{
    private readonly Mock<IInventoryRepository> _inventoryRepositoryMock;
    private readonly InventoryService _inventoryService;

    public InventoryServiceTests()
    {
        _inventoryRepositoryMock = new Mock<IInventoryRepository>();
        _inventoryService = new InventoryService(_inventoryRepositoryMock.Object);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenEnoughInventory_ShouldReturnSuccess()
    {
        // Arrange
        var productId = "product-1";
        var quantityToDeduct = 10;
        var remainingQuantity = 90;
        _inventoryRepositoryMock
            .Setup(x => x.DeductInventoryAsync(productId, quantityToDeduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingQuantity);

        // Act
        var result = await _inventoryService.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.RemainingQuantity.Should().Be(remainingQuantity);
        result.DeductedQuantity.Should().Be(quantityToDeduct);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenNotEnoughInventory_ShouldReturnIsSuccessFalse()
    {
        // Arrange
        var productId = "product-1";
        var quantityToDeduct = 100;
        _inventoryRepositoryMock
            .Setup(x => x.DeductInventoryAsync(productId, quantityToDeduct, It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        // Act
        var result = await _inventoryService.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.RemainingQuantity.Should().Be(0);
        result.DeductedQuantity.Should().Be(0);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenProductIdNullOrEmpty_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _inventoryService.DeductInventoryAsync(string.Empty, 10);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*ProductId không được để trống*");
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenQuantityToDeductZero_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _inventoryService.DeductInventoryAsync("product-1", 0);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*QuantityToDeduct phải lớn hơn 0*");
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenQuantityToDeductNegative_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => _inventoryService.DeductInventoryAsync("product-1", -10);

        // Assert
        (await act.Should().ThrowAsync<ArgumentException>())
            .WithMessage("*QuantityToDeduct phải lớn hơn 0*");
    }
}
