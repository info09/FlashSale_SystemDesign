using FlashSale.Application.Features.Order.Commands;
using FluentValidation;

namespace FlashSale.Application.Features.Order.Validators;

/// <summary>
/// Validator cho CreateOrderCommand sử dụng FluentValidation.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: Order.
/// </summary>
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
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
