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

    /// <summary>Khởi tạo handler với dịch vụ Hold.</summary>
    public ConfirmHoldCommandHandler(IHoldService holdService)
    {
        _holdService = holdService;
    }

    /// <summary>Xử lý lệnh confirm hold bằng cách delegate sang <see cref="IHoldService"/>.</summary>
    public async Task<ConfirmHoldResult> Handle(ConfirmHoldCommand request, CancellationToken cancellationToken)
    {
        return await _holdService.ConfirmHoldAsync(request, cancellationToken);
    }
}
