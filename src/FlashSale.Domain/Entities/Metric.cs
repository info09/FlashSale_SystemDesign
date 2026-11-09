namespace FlashSale.Domain.Entities;

/// <summary>
/// Ket qua mot chi so hieu suat (Metric) duoc xuat ra de Prometheus scrape.
/// Thuat ngua chuan theo docs/domain-glossary.md: Metric, P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining.
/// Khong duoc goi la: Metrics, Kpi, Indicator.
/// </summary>
public record Metric
{
    /// <summary>Ten chi so (P99Latency, ErrorRate, QueueDepth, DlqSize, InventoryRemaining).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gia tri chi so.</summary>
    public double Value { get; init; }

    /// <summary>Don vi tinh (ms, percent, count, v.v.).</summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>Thoi gian lay mau.</summary>
    public DateTimeOffset CollectedAt { get; init; } = DateTimeOffset.UtcNow;
}
