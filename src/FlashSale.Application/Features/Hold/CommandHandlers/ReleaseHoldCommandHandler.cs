using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Hold.CommandHandlers;

/// <summary>
/// CommandHandler xử lý ReleaseHoldCommand, delegate sang IHoldService.
/// Thuật ngữ chuẩn: Hold.
/// </summary>
public class ReleaseHoldCommandHandler : IRequestHandler<ReleaseHoldCommand, ReleaseHoldResult>
{
    private readonly IHoldService _holdService;

    /// <summary>Khởi tạo handler với dịch vụ Hold.</summary>
    public ReleaseHoldCommandHandler(IHoldService holdService)
    {
        _holdService = holdService;
    }

    /// <summary>Xử lý lệnh release hold bằng cách delegate sang <see cref="IHoldService"/>.</summary>
    public async Task<ReleaseHoldResult> Handle(ReleaseHoldCommand request, CancellationToken cancellationToken)
    {
        return await _holdService.ReleaseHoldAsync(request, cancellationToken);
    }
}
