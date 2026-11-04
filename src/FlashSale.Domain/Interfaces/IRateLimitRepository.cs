using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Interfaces;

/// <summary>
/// Giao diện định nghĩa thao tác kiểm tra và trừ lượt RateLimit trên Redis.
/// Ở lớp Domain chỉ định nghĩa契約 (contract), chưa viết logic thực thi.
/// 
/// Thuật ngữ chuẩn theo docs/domain-glossary.md: RateLimit, IRateLimitRepository.
/// </summary>
public interface IRateLimitRepository
{
    /// <summary>
    /// Kiểm tra xem client có được phép gọi API không và trừ 1 lượt nếu được phép.
    /// Thực thi nguyên tử (atomic) qua Redis Lua Script (Token Bucket).
    /// </summary>
    /// <param name="clientId">Mã định danh khách hàng gọi API.</param>
    /// <param name="maxRequests">Số lượng request tối đa cho phép trong 1 cửa sổ thời gian.</param>
    /// <param name="windowSeconds">Khoảng thời gian (giây) của cửa sổ rate limit.</param>
    /// <param name="cancellationToken">Token hủy bỏ thao tác bất đồng bộ.</param>
    /// <returns>Kết quả RateLimit cho client.</returns>
    Task<RateLimit> CheckAndConsumeAsync(string clientId, int maxRequests, int windowSeconds, CancellationToken cancellationToken = default);
}
