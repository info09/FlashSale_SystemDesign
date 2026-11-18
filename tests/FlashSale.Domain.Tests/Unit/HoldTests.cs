using FlashSale.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FlashSale.Domain.Tests.Unit;

public class HoldTests
{
    [Fact]
    public void Hold_CreateActive_ShouldSetProperties()
    {
        var holdId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(60);

        var hold = new Hold(holdId, "product-1", 2, "client-1", expiresAt);

        hold.Id.Should().Be(holdId);
        hold.ProductId.Should().Be("product-1");
        hold.Quantity.Should().Be(2);
        hold.ClientId.Should().Be("client-1");
        hold.ExpiresAt.Should().Be(expiresAt);
        hold.Status.Should().Be(HoldStatus.Active);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Hold_CreateWithInvalidProductId_ShouldThrow(string? productId)
    {
        var act = () => new Hold(Guid.NewGuid(), productId!, 1, "client-1", DateTimeOffset.UtcNow.AddSeconds(60));

        act.Should().Throw<ArgumentException>().WithMessage("*ProductId*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Hold_CreateWithInvalidQuantity_ShouldThrow(int quantity)
    {
        var act = () => new Hold(Guid.NewGuid(), "product-1", quantity, "client-1", DateTimeOffset.UtcNow.AddSeconds(60));

        act.Should().Throw<ArgumentException>().WithMessage("*Quantity*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Hold_CreateWithInvalidClientId_ShouldThrow(string? clientId)
    {
        var act = () => new Hold(Guid.NewGuid(), "product-1", 1, clientId!, DateTimeOffset.UtcNow.AddSeconds(60));

        act.Should().Throw<ArgumentException>().WithMessage("*ClientId*");
    }

    [Fact]
    public void Hold_IsExpired_WhenNowAfterExpiresAt_ShouldReturnTrue()
    {
        var hold = new Hold(Guid.NewGuid(), "product-1", 1, "client-1", DateTimeOffset.UtcNow.AddSeconds(-1));

        hold.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void Hold_IsExpired_WhenNowBeforeExpiresAt_ShouldReturnFalse()
    {
        var hold = new Hold(Guid.NewGuid(), "product-1", 1, "client-1", DateTimeOffset.UtcNow.AddSeconds(30));

        hold.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Hold_MarkConsumed_ShouldSetStatusConsumed()
    {
        var hold = new Hold(Guid.NewGuid(), "product-1", 1, "client-1", DateTimeOffset.UtcNow.AddSeconds(60));

        hold.MarkConsumed();

        hold.Status.Should().Be(HoldStatus.Consumed);
    }

    [Fact]
    public void Hold_MarkReleased_ShouldSetStatusReleased()
    {
        var hold = new Hold(Guid.NewGuid(), "product-1", 1, "client-1", DateTimeOffset.UtcNow.AddSeconds(60));

        hold.MarkReleased();

        hold.Status.Should().Be(HoldStatus.Released);
    }
}
