using StackExchange.Redis;

namespace FlashSale.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Giao dien thuc thi Lua Script tren Redis de co the mock trong unit test.
/// </summary>
public interface IRedisScriptExecutor
{
    /// <summary>
    /// Chay Lua Script nguyen tu tren Redis.
    /// </summary>
    Task<string> EvaluateAsync(string script, RedisKey[] keys, RedisValue[] args, CancellationToken cancellationToken = default);
}
