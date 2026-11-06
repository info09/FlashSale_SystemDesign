using System;

namespace FlashSale.Application.Services;

/// <summary>
/// Chinh sach Retry voi Exponential Backoff.
/// Tinh toan thoi gian cho trong tung lan thu lai theo cong thuc: baseDelay * 2^(retryCount - 1).
/// 
/// Thuat ngua chuan theo docs/domain-glossary.md: ExponentialBackoff, Retry.
/// </summary>
public class ExponentialBackoffPolicy
{
    private readonly int _maxRetryCount;
    private readonly TimeSpan _baseDelay;

    public ExponentialBackoffPolicy(int maxRetryCount = 3, TimeSpan? baseDelay = null)
    {
        if (maxRetryCount <= 0)
            throw new ArgumentException("MaxRetryCount phai lon hon 0", nameof(maxRetryCount));

        _maxRetryCount = maxRetryCount;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Kiem tra co the thu lai khong (retryCount chua vuot qua gioi han toi da).
    /// </summary>
    public bool CanRetry(int retryCount) => retryCount < _maxRetryCount;

    /// <summary>
    /// Tinh toan thoi gian cho trong cho lan thu lai thu retryCount.
    /// retryCount la 0-based (lan that bai dau tien = 0, lan thu lai dau tien).
    /// Cong thuc: baseDelay * 2^retryCount
    /// </summary>
    public TimeSpan GetDelay(int retryCount)
    {
        if (retryCount < 0)
            throw new ArgumentException("RetryCount phai lon hon hoac bang 0", nameof(retryCount));

        var delayMs = _baseDelay.TotalMilliseconds * Math.Pow(2, retryCount);
        return TimeSpan.FromMilliseconds(delayMs);
    }

    /// <summary>
    /// So lan thu lai toi da duoc phep.
    /// </summary>
    public int MaxRetryCount => _maxRetryCount;
}
