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

    public CreateHoldCommandHandler(IHoldService holdService)
    {
        _holdService = holdService;
    }

    public async Task<CreateHoldResult> Handle(CreateHoldCommand request, CancellationToken cancellationToken)
    {
        return await _holdService.CreateHoldAsync(request, cancellationToken);
    }
}
