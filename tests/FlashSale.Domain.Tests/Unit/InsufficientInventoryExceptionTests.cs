using FluentAssertions;
using FlashSale.Domain.Exceptions;
using Xunit;

namespace FlashSale.Domain.Tests.Unit;

public class InsufficientInventoryExceptionTests
{
    [Fact]
    public void InsufficientInventoryException_Create_ShouldSetProperties()
    {
        // Arrange & Act
        var exception = new InsufficientInventoryException("product-1", 50, 10);

        // Assert
        exception.ProductId.Should().Be("product-1");
        exception.RequestedQuantity.Should().Be(50);
        exception.AvailableQuantity.Should().Be(10);
        exception.Message.Should().Contain("product-1");
        exception.Message.Should().Contain("50");
        exception.Message.Should().Contain("10");
        exception.Message.Should().Contain("OverSelling");
    }

    [Fact]
    public void InsufficientInventoryException_Message_ShouldContainOverSelling()
    {
        // Arrange & Act
        var exception = new InsufficientInventoryException("product-1", 50, 10);

        // Assert
        exception.Message.Should().Contain("OverSelling");
    }
}
