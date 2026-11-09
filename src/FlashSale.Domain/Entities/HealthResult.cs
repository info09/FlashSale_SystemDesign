namespace FlashSale.Domain.Entities;

/// <summary>
/// Ket qua cua quy trinh HealthCheck: xac nhan tinh trang suc khoe cua he thong va tung dich vao.
/// Thuat ngua chuan theo docs/domain-glossary.md: HealthCheck, DataConsistency.
/// </summary>
public record HealthResult
{
    /// <summary>Tinh trang tong the: Healthy hoac Unhealthy.</summary>
    public string Status { get; init; } = "Unhealthy";

    /// <summary>Tinh trang cua tung dich vao (PostgreSQLPrimary, Redis, RabbitMQ).</summary>
    public Dictionary<string, object> Checks { get; init; } = new();

    /// <summary>Co tat ca dich vao khong khoe khong.</summary>
    public bool IsHealthy => Checks.Values.OfType<string>().All(v => v == "Healthy");
}
