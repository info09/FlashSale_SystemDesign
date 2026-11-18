using FlashSale.Application.Features.Hold.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.Features.Hold.CommandHandlers;

/// <summary>
/// CommandHandler xử lý CreateHoldCommand, delegate sang IHoldService.
/// Thuật ngữ chuẩn: Hold.
/// </summary>
public class CreateHoldCommandHandler : IRequestHandler<CreateHoldCommand, CreateHoldResult>
{
    private readonly IHoldService _holdService;

    /// <summary>Khởi tạo handler với dịch vụ Hold.</summary>
    public CreateHoldCommandHandler(IHoldService holdService)
    {
        _holdService = holdService;
    }

    /// <summary>Xử lý lệnh tạo hold bằng cách delegate sang <see cref="IHoldService"/>.</summary>
    public async Task<CreateHoldResult> Handle(CreateHoldCommand request, CancellationToken cancellationToken)
    {
        return await _holdService.CreateHoldAsync(request, cancellationToken);
    }
}
