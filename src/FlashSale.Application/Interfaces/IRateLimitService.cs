using FlashSale.Application.Features.RateLimit.Commands;

namespace FlashSale.Application.Interfaces;

/// <summary>
/// Giao diện dịch vụ RateLimit ở lớp Application.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Kiểm tra RateLimit cho client và trả về kết quả cho phép/từ chối.
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(string clientId, int maxRequests, int windowSeconds, CancellationToken cancellationToken = default);
}
