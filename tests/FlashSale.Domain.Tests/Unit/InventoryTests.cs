using FluentAssertions;
using FlashSale.Domain.Entities;
using Xunit;

namespace FlashSale.Domain.Tests.Unit;

public class InventoryTests
{
    [Fact]
    public void Inventory_CreateWithValidQuantity_ShouldSetProperties()
    {
        // Arrange & Act
        var inventory = new Inventory("product-1", 100);

        // Assert
        inventory.ProductId.Should().Be("product-1");
        inventory.Quantity.Should().Be(100);
        inventory.ReservedQuantity.Should().Be(0);
        inventory.AvailableQuantity.Should().Be(100);
    }

    [Fact]
    public void Inventory_CreateWithEmptyProductId_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new Inventory("", 100);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ProductId không được để trống*");
    }

    [Fact]
    public void Inventory_CreateWithNullProductId_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new Inventory(null!, 100);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ProductId không được để trống*");
    }

    [Fact]
    public void Inventory_CreateWithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange & Act
        var act = () => new Inventory("product-1", -10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Quantity không được âm*");
    }

    [Fact]
    public void Inventory_SetQuantity_ShouldUpdateAvailableQuantity()
    {
        // Arrange
        var inventory = new Inventory("product-1", 100);

        // Act
        inventory.SetQuantity(200);

        // Assert
        inventory.Quantity.Should().Be(200);
        inventory.AvailableQuantity.Should().Be(200);
    }

    [Fact]
    public void Inventory_SetReservedQuantity_ShouldUpdateAvailableQuantity()
    {
        // Arrange
        var inventory = new Inventory("product-1", 100);

        // Act
        inventory.SetReservedQuantity(30);

        // Assert
        inventory.ReservedQuantity.Should().Be(30);
        inventory.AvailableQuantity.Should().Be(70);
    }

    [Fact]
    public void Inventory_HasAvailableQuantity_WhenEnoughStock_ShouldReturnTrue()
    {
        // Arrange
        var inventory = new Inventory("product-1", 100);

        // Act
        var result = inventory.HasAvailableQuantity(50);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Inventory_HasAvailableQuantity_WhenNotEnoughStock_ShouldReturnFalse()
    {
        // Arrange
        var inventory = new Inventory("product-1", 10);

        // Act
        var result = inventory.HasAvailableQuantity(50);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Inventory_HasAvailableQuantity_WhenZeroRequested_ShouldReturnFalse()
    {
        // Arrange
        var inventory = new Inventory("product-1", 100);

        // Act
        var result = inventory.HasAvailableQuantity(0);

        // Assert
        result.Should().BeFalse();
    }
}
