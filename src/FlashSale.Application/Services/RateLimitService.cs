using FlashSale.Application.Commands;
using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Exceptions;
using FlashSale.Domain.Interfaces;

namespace FlashSale.Application.Services;

/// <summary>
/// Dịch vụ RateLimit ở lớp Application, orchestrates việc kiểm tra RateLimit qua Domain Repository.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit, RateLimitExceededException.
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IRateLimitRepository _rateLimitRepository;

    public RateLimitService(IRateLimitRepository rateLimitRepository)
    {
        _rateLimitRepository = rateLimitRepository;
    }

    /// <summary>
    /// Kiểm tra RateLimit cho client. Nếu vượt ngưỡng, ném RateLimitExceededException
    /// để ApiMiddleware trả HTTP 429 cho client (FR-01, NFR-04).
    /// </summary>
    public async Task<RateLimitResult> CheckRateLimitAsync(string clientId, int maxRequests, int windowSeconds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("ClientId không được để trống", nameof(clientId));
        }

        if (maxRequests <= 0)
        {
            throw new ArgumentException("MaxRequests phải lớn hơn 0", nameof(maxRequests));
        }

        if (windowSeconds <= 0)
        {
            throw new ArgumentException("WindowSeconds phải lớn hơn 0", nameof(windowSeconds));
        }

        // Gọi Domain Repository để thực hiện check-and-consume atomic qua Redis Lua Script
        var rateLimit = await _rateLimitRepository.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds, cancellationToken);

        if (!rateLimit.IsAllowed)
        {
            // Ném exception để ApiMiddleware bắt và trả HTTP 429
            throw new RateLimitExceededException(rateLimit.ClientId, rateLimit.ResetAfterSeconds);
        }

        return new RateLimitResult(
            IsAllowed: true,
            RemainingRequests: rateLimit.RemainingRequests,
            ResetAfterSeconds: rateLimit.ResetAfterSeconds
        );
    }
}
