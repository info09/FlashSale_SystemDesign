using FlashSale.Application.Features.Hold.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Hold.Validators;

public class CreateHoldCommandValidator : AbstractValidator<CreateHoldCommand>
{
    public CreateHoldCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId không được để trống");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity phải lớn hơn 0");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId không được để trống");
    }
}
