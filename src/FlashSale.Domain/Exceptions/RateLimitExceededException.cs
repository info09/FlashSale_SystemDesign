namespace FlashSale.Domain.Exceptions;

/// <summary>
/// Ngoại lệ ném khi client vượt ngưỡng RateLimit (đã hết lượt trong cửa sổ thời gian).
/// Controller sẽ bắt ngoại lệ này và trả HTTP 429 Too Many Requests cho client (FR-01, NFR-04).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit, RateLimitExceededException (không được gọi là ThrottlingException, QuotaExceededException).
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>Mã định danh khách hàng bị chặn do vượt rate limit.</summary>
    public string ClientId { get; }

    /// <summary>Thời gian (giây) khách hàng phải đợi trước khi được phép gọi lại.</summary>
    public int ResetAfterSeconds { get; }

    public RateLimitExceededException(string clientId, int resetAfterSeconds)
        : base($"Rate limit exceeded for client '{clientId}'. Try again after {resetAfterSeconds} seconds.")
    {
        ClientId = clientId;
        ResetAfterSeconds = resetAfterSeconds;
    }
}
