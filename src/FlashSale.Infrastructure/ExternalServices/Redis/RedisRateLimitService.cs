using StackExchange.Redis;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Exceptions;

namespace FlashSale.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Trien khai IRateLimitRepository dung Redis Lua Script (Token Bucket) nguyen tu.
/// Thuat ngua chuan theo docs/domain-glossary.md: RateLimit, TokenBucket, LuaScript, Redis.
/// Khong duoc goi la ThrottlingService, RateLimiter, RedisService.
/// </summary>
public class RedisRateLimitService : IRateLimitRepository
{
    private readonly IRedisScriptExecutor _scriptExecutor;
    private readonly string _luaScript;

    public RedisRateLimitService(IRedisScriptExecutor scriptExecutor, string luaScriptContent)
    {
        _scriptExecutor = scriptExecutor;
        _luaScript = luaScriptContent;
    }

    /// <summary>
    /// Kiem tra va tru luot RateLimit nguyen tu qua Redis Lua Script (Token Bucket).
    /// </summary>
    public async Task<RateLimit> CheckAndConsumeAsync(string clientId, int maxRequests, int windowSeconds, CancellationToken cancellationToken = default)
    {
        var key = $"rate_limit:{clientId}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Chay Lua Script nguyen tu tren Redis (khong chia thanh nhieu lenh)
        var luaResult = await _scriptExecutor.EvaluateAsync(
            _luaScript,
            new RedisKey[] { key },
            new RedisValue[] { maxRequests, windowSeconds, now },
            cancellationToken
        );

        // Parse ket qua Lua tra ve: { allowed, remaining, resetAfter }
        var parts = luaResult.Split(',');
        var allowed = int.Parse(parts[0]);
        var remainingRequests = int.Parse(parts[1]);
        var resetAfterSeconds = int.Parse(parts[2]);

        if (allowed == 1)
        {
            return RateLimit.Allowed(clientId, remainingRequests, resetAfterSeconds);
        }

        return RateLimit.Rejected(clientId, remainingRequests, resetAfterSeconds);
    }
}
