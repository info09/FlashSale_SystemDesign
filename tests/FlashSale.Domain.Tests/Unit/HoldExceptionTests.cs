using FlashSale.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace FlashSale.Domain.Tests.Unit;

public class HoldExceptionTests
{
    [Fact]
    public void HoldExpiredException_ShouldContainHoldId()
    {
        var holdId = Guid.NewGuid();

        var exception = new HoldExpiredException(holdId);

        exception.HoldId.Should().Be(holdId);
        exception.Message.Should().Contain(holdId.ToString());
    }

    [Fact]
    public void HoldInvalidException_ShouldContainHoldId()
    {
        var holdId = Guid.NewGuid();

        var exception = new HoldInvalidException(holdId);

        exception.HoldId.Should().Be(holdId);
        exception.Message.Should().Contain(holdId.ToString());
    }

    [Fact]
    public void HoldConsumedException_ShouldContainHoldId()
    {
        var holdId = Guid.NewGuid();

        var exception = new HoldConsumedException(holdId);

        exception.HoldId.Should().Be(holdId);
        exception.Message.Should().Contain(holdId.ToString());
    }

    [Fact]
    public void HoldNotOwnedException_ShouldContainHoldIdAndClientId()
    {
        var holdId = Guid.NewGuid();
        var clientId = "client-1";

        var exception = new HoldNotOwnedException(holdId, clientId);

        exception.HoldId.Should().Be(holdId);
        exception.ClientId.Should().Be(clientId);
        exception.Message.Should().Contain(holdId.ToString());
        exception.Message.Should().Contain(clientId);
    }

    [Fact]
    public void HoldAlreadyConfirmedException_ShouldContainHoldId()
    {
        var holdId = Guid.NewGuid();

        var exception = new HoldAlreadyConfirmedException(holdId);

        exception.HoldId.Should().Be(holdId);
        exception.Message.Should().Contain(holdId.ToString());
    }

    [Fact]
    public void ActiveHoldAlreadyExistsException_ShouldContainClientIdAndHoldId()
    {
        var clientId = "client-1";
        var holdId = Guid.NewGuid();

        var exception = new ActiveHoldAlreadyExistsException(clientId, holdId);

        exception.ClientId.Should().Be(clientId);
        exception.ExistingHoldId.Should().Be(holdId);
        exception.Message.Should().Contain(clientId);
        exception.Message.Should().Contain(holdId.ToString());
    }
}
