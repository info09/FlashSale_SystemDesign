namespace FlashSale.Domain.Entities;

/// <summary>
/// Entity nghiệp vụ đại diện cho kết quả kiểm tra RateLimit của một Client.
/// Được dùng trong TrafficProtection Module (FR-01, NFR-04).
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit (không được gọi là Throttling, Quota, RateLimiter).
/// </summary>
public class RateLimit
{
    /// <summary>Mã định danh khách hàng (client) gọi API.</summary>
    public string ClientId { get; private set; }

    /// <summary>Số lượng request còn lại trong cửa sổ thời gian hiện tại.</summary>
    public int RemainingRequests { get; private set; }

    /// <summary>Thời gian (giây) còn lại trước khi quota rate limit được reset lại.</summary>
    public int ResetAfterSeconds { get; private set; }

    /// <summary>Có được phép tiếp tục xử lý request hay không.</summary>
    public bool IsAllowed { get; private set; }

    private RateLimit() { }

    public RateLimit(string clientId, int remainingRequests, int resetAfterSeconds, bool isAllowed)
    {
        ClientId = clientId;
        RemainingRequests = remainingRequests;
        ResetAfterSeconds = resetAfterSeconds;
        IsAllowed = isAllowed;
    }

    /// <summary>
    /// Tạo đối tượng RateLimit cho trường hợp client được phép gọi (vẫn còn lượt).
    /// </summary>
    public static RateLimit Allowed(string clientId, int remainingRequests, int resetAfterSeconds)
    {
        return new RateLimit(clientId, remainingRequests, resetAfterSeconds, true);
    }

    /// <summary>
    /// Tạo đối tượng RateLimit cho trường hợp client bị từ chối (đã hết lượt).
    /// </summary>
    public static RateLimit Rejected(string clientId, int remainingRequests, int resetAfterSeconds)
    {
        return new RateLimit(clientId, remainingRequests, resetAfterSeconds, false);
    }
}
