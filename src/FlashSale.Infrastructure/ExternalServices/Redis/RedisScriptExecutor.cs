using StackExchange.Redis;

namespace FlashSale.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Trien khai IRedisScriptExecutor su dung StackExchange.Redis.
/// </summary>
public class RedisScriptExecutor : IRedisScriptExecutor
{
    private readonly IDatabase _redis;

    public RedisScriptExecutor(IDatabase redis)
    {
        _redis = redis;
    }

    public async Task<string> EvaluateAsync(string script, RedisKey[] keys, RedisValue[] args, CancellationToken cancellationToken = default)
    {
        var result = await _redis.ScriptEvaluateAsync(script, keys, args, flags: CommandFlags.None);
        return result.ToString();
    }
}
