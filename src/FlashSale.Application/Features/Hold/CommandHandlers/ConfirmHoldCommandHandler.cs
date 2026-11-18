using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Hold.CommandHandlers;

/// <summary>
/// CommandHandler xử lý ConfirmHoldCommand, delegate sang IHoldService.
/// Thuật ngữ chuẩn: Hold, InformationCustomer.
/// </summary>
public class ConfirmHoldCommandHandler : IRequestHandler<ConfirmHoldCommand, ConfirmHoldResult>
{
    private readonly IHoldService _holdService;

    public ConfirmHoldCommandHandler(IHoldService holdService)
    {
        _holdService = holdService;
    }

    public async Task<ConfirmHoldResult> Handle(ConfirmHoldCommand request, CancellationToken cancellationToken)
    {
        return await _holdService.ConfirmHoldAsync(request, cancellationToken);
    }
}
