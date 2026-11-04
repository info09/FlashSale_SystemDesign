using FlashSale.Application.Commands;
using FlashSale.Application.Interfaces;
using MediatR;

namespace FlashSale.Application.CommandHandlers;

/// <summary>
/// CommandHandler xử lý RateLimitCommand, delegate sang IRateLimitService.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit.
/// </summary>
public class RateLimitCommandHandler : IRequestHandler<RateLimitCommand, RateLimitResult>
{
    private readonly IRateLimitService _rateLimitService;

    public RateLimitCommandHandler(IRateLimitService rateLimitService)
    {
        _rateLimitService = rateLimitService;
    }

    public async Task<RateLimitResult> Handle(RateLimitCommand request, CancellationToken cancellationToken)
    {
        return await _rateLimitService.CheckRateLimitAsync(
            request.ClientId,
            request.MaxRequests,
            request.WindowSeconds,
            cancellationToken);
    }
}
