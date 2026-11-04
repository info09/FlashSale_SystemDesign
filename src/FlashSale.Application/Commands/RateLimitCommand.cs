using MediatR;

namespace FlashSale.Application.Commands;

/// <summary>
/// Command yêu cầu kiểm tra RateLimit trước khi xử lý request mua hàng.
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit, ClientId.
/// 
/// [CẦN LÀM RÕ] Ngưỡng rate limit cụ thể là bao nhiêu request/giây per customer hay per IP
/// (docs/prd.md Câu 1 Open Questions). Hiện tại để MaxRequests, WindowSeconds làm tham số đầu vào,
/// sẽ được cấu hình cụ thể sau khi có câu trả lời.
/// </summary>
public record RateLimitCommand(string ClientId, int MaxRequests, int WindowSeconds) : IRequest<RateLimitResult>;

/// <summary>
/// Kết quả trả về sau khi kiểm tra RateLimit.
/// </summary>
public record RateLimitResult(bool IsAllowed, int RemainingRequests, int ResetAfterSeconds);
