using FlashSale.Domain.Entities;

namespace FlashSale.Domain.Models;

/// <summary>
/// Kết quả consume/confirm Hold từ kho lưu trữ (Redis atomic).
/// </summary>
public sealed record HoldConsumeResult(HoldConsumeStatus Status, Hold? Hold = null);
