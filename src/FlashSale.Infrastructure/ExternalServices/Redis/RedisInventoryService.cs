using StackExchange.Redis;
using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Exceptions;

namespace FlashSale.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Trien khai IInventoryRepository dung Redis Lua Script (AtomicInventoryOffloading) nguyen tu.
/// Thuat ngua chuan theo docs/domain-glossary.md: Inventory, AtomicInventoryOffloading, LuaScript, Redis.
/// Khong duoc goi la: IStockRepository, IStockService, RedisService.
/// </summary>
public class RedisInventoryService : IInventoryRepository
{
    private readonly IRedisScriptExecutor _scriptExecutor;
    private readonly string _luaScript;

    public RedisInventoryService(IRedisScriptExecutor scriptExecutor, string luaScriptContent)
    {
        _scriptExecutor = scriptExecutor;
        _luaScript = luaScriptContent;
    }

    /// <summary>
    /// Lay so luong ton kho hien tai cua mot Product tu Redis.
    /// </summary>
    public async Task<int?> GetInventoryAsync(string productId, CancellationToken cancellationToken = default)
    {
        var key = $"inventory:{productId}";
        var result = await _scriptExecutor.EvaluateAsync(
            "return tonumber(redis.call('GET', KEYS[1]) or '0')",
            new RedisKey[] { key },
            new RedisValue[] { },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            return null;

        return int.Parse(result);
    }

    /// <summary>
    /// Tru kho atomic qua Redis Lua Script (AtomicInventoryOffloading).
    /// Tra ve so luong con lai sau khi tru, hoac null neu khong du kho.
    /// </summary>
    public async Task<int?> DeductInventoryAsync(string productId, int quantityToDeduct, CancellationToken cancellationToken = default)
    {
        var key = $"inventory:{productId}";

        // Chay Lua Script nguyen tu tren Redis
        var result = await _scriptExecutor.EvaluateAsync(
            _luaScript,
            new RedisKey[] { key },
            new RedisValue[] { quantityToDeduct },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result) || result == "nil")
            return null; // Khong du kho

        return int.Parse(result);
    }

    /// <summary>
    /// Dat so luong ton kho ban dau cho mot Product (dung khi khoi tao kho).
    /// </summary>
    public async Task SetInventoryAsync(string productId, int quantity, CancellationToken cancellationToken = default)
    {
        var key = $"inventory:{productId}";
        await _scriptExecutor.EvaluateAsync(
            "redis.call('SET', KEYS[1], ARGV[1])",
            new RedisKey[] { key },
            new RedisValue[] { quantity },
            cancellationToken);
    }
}
