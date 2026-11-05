using StackExchange.Redis;
using System.Linq;
using System.Reflection;

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

        // Lua script co the tra ve nil (vi du: Inventory het kho).
        if (result.IsNull)
            return null;

        // Lua script co the tra ve mang [allowed, remaining, resetAfter] hoac mot gia tri don le.
        // StackExchange.Redis bung mang Lua vao mot RedisResult. Dung reflection de lay
        // truong _result chua mang RedisResult[].
        var resultField = result.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(f => f.FieldType.IsArray);
        var innerResult = resultField?.GetValue(result);

        if (innerResult is RedisResult[] array)
        {
            var values = array.Select(e => e.ToString()).ToArray();
            return string.Join(",", values);
        }

        return result.ToString();
    }
}
