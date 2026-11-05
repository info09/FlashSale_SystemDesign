using FluentValidation;
using FlashSale.Application.Features.Inventory.Commands;

namespace FlashSale.Application.Features.Inventory.Validators;

/// <summary>
/// Validator cho AddInventoryCommand sử dụng FluentValidation.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Inventory.
/// </summary>
public class AddInventoryCommandValidator : AbstractValidator<AddInventoryCommand>
{
    public AddInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId không được để trống");

        RuleFor(x => x.QuantityToAdd)
            .GreaterThan(0)
            .WithMessage("QuantityToAdd phải lớn hơn 0");
    }
}
