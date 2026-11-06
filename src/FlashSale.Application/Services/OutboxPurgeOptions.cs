namespace FlashSale.Application.Services;

/// <summary>
/// Cau hinh thoi gian luu tru bang Outbox truoc khi purge/archive (NFR-06).
/// Thuat ngua chuan theo docs/domain-glossary.md: Outbox, Purge.
/// </summary>
public class OutboxPurgeOptions
{
    /// <summary>So ngay luu tru ghi Outbox trong PostgreSQLPrimary truoc khi purge (mac dinh 30 ngay).</summary>
    public int PurgeDays { get; set; } = 30;
}
