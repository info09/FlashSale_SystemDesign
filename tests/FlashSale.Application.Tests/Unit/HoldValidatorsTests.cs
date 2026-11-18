using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Features.Hold.Validators;
using FluentAssertions;
using Xunit;

namespace FlashSale.Application.Tests.Unit;

public class HoldValidatorsTests
{
    [Fact]
    public void CreateHoldCommandValidator_Invalid_ShouldHaveErrors()
    {
        var validator = new CreateHoldCommandValidator();

        var result = validator.Validate(new CreateHoldCommand("", 0, ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }

    [Fact]
    public void CreateHoldCommandValidator_Valid_ShouldPass()
    {
        var validator = new CreateHoldCommandValidator();

        var result = validator.Validate(new CreateHoldCommand("p1", 1, "c1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ConfirmHoldCommandValidator_Invalid_ShouldHaveErrors()
    {
        var validator = new ConfirmHoldCommandValidator();

        var result = validator.Validate(new ConfirmHoldCommand(Guid.Empty, "", "", "", ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HoldId");
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
        result.Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public void ConfirmHoldCommandValidator_Valid_ShouldPass()
    {
        var validator = new ConfirmHoldCommandValidator();

        var result = validator.Validate(new ConfirmHoldCommand(Guid.NewGuid(), "c1", "Name", "0900", "Addr"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReleaseHoldCommandValidator_Invalid_ShouldHaveErrors()
    {
        var validator = new ReleaseHoldCommandValidator();

        var result = validator.Validate(new ReleaseHoldCommand(Guid.Empty, ""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "HoldId");
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }

    [Fact]
    public void ReleaseHoldCommandValidator_Valid_ShouldPass()
    {
        var validator = new ReleaseHoldCommandValidator();

        var result = validator.Validate(new ReleaseHoldCommand(Guid.NewGuid(), "c1"));

        result.IsValid.Should().BeTrue();
    }
}
