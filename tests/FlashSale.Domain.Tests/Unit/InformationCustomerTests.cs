using FlashSale.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace FlashSale.Domain.Tests.Unit;

public class InformationCustomerTests
{
    [Fact]
    public void InformationCustomer_Create_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var holdId = Guid.NewGuid();

        var info = new InformationCustomer(id, "Nguyen Van A", "Ha Noi", "0900000000", holdId);

        info.Id.Should().Be(id);
        info.Name.Should().Be("Nguyen Van A");
        info.Address.Should().Be("Ha Noi");
        info.PhoneNumber.Should().Be("0900000000");
        info.HoldId.Should().Be(holdId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void InformationCustomer_InvalidName_ShouldThrow(string? name)
    {
        var act = () => new InformationCustomer(Guid.NewGuid(), name!, "Ha Noi", "0900000000", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*Name*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void InformationCustomer_InvalidAddress_ShouldThrow(string? address)
    {
        var act = () => new InformationCustomer(Guid.NewGuid(), "A", address!, "0900000000", Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*Address*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void InformationCustomer_InvalidPhoneNumber_ShouldThrow(string? phone)
    {
        var act = () => new InformationCustomer(Guid.NewGuid(), "A", "Ha Noi", phone!, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*PhoneNumber*");
    }

    [Fact]
    public void InformationCustomer_EmptyHoldId_ShouldThrow()
    {
        var act = () => new InformationCustomer(Guid.NewGuid(), "A", "Ha Noi", "0900000000", Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*HoldId*");
    }
}
