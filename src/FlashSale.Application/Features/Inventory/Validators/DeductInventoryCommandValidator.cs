using FlashSale.Application.Features.Inventory.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Inventory.Validators;

/// <summary>
/// Validator cho DeductInventoryCommand sử dụng FluentValidation.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory.
/// </summary>
public class DeductInventoryCommandValidator : AbstractValidator<DeductInventoryCommand>
{
    public DeductInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId không được để trống");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity phải lớn hơn 0");

        RuleFor(x => x.ClientId)
            .NotEmpty()
            .WithMessage("ClientId không được để trống");
    }
}
