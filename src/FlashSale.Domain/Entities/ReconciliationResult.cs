using System.Collections.Generic;

namespace FlashSale.Domain.Entities;

/// <summary>
/// Ket qua cua quy trinh Reconciliation (doi chieu du lieu): xac nhan 100% don 202 Accepted da co ban ghi trong PostgreSQLPrimary.
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: Reconciliation, DataConsistency, Order, Outbox.
/// </summary>
public record ReconciliationResult
{
    /// <summary>Tong so ghi Outbox da kiem tra (don da nhan 202 Accepted).</summary>
    public int TotalChecked { get; init; }

    /// <summary>So don da co trung lap giua Order va Outbox (da ghi thanh cong vao PostgreSQLPrimary).</summary>
    public int MatchedCount { get; init; }

    /// <summary>So don da nhan 202 Accepted (co Outbox) ma chua co Order trong PostgreSQLPrimary (don bi mat).</summary>
    public int MissingOrderCount { get; init; }

    /// <summary>Danh sach CorrelationId cua cac don bi mat (can can thiep / retry).</summary>
    public List<Guid> MissingCorrelationIds { get; init; } = new();

    /// <summary>Co thuc su du lieu lien thong khong (MissingOrderCount = 0).</summary>
    public bool IsConsistent => MissingOrderCount == 0;
}
