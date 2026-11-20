using FlashSale.Domain.Entities;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Models;
using StackExchange.Redis;

namespace FlashSale.Infrastructure.ExternalServices.Redis;

/// <summary>
/// Triển khai IHoldRepository dùng Redis Lua Script atomic.
/// Thuật ngữ chuẩn: Hold, HoldExpiry, Inventory, WorkerUnHold.
/// </summary>
public class RedisHoldService : IHoldRepository
{
    private const int DefaultTtlSeconds = 60;

    private readonly IRedisScriptExecutor _scriptExecutor;
    private readonly string _createHoldScript;
    private readonly string _confirmHoldScript;
    private readonly string _releaseHoldScript;
    private readonly IDatabase _redisDatabase;

    /// <summary>
    /// Khởi tạo RedisHoldService với executor, nội dung Lua script và Redis database.
    /// </summary>
    /// <param name="scriptExecutor">Executor thực thi Lua script trên Redis.</param>
    /// <param name="createHoldScript">Lua script tạo hold/reserve inventory atomic.</param>
    /// <param name="confirmHoldScript">Lua script consume/confirm hold atomic.</param>
    /// <param name="releaseHoldScript">Lua script release hold và trả inventory (idempotent).</param>
    /// <param name="redisDatabase">Redis database dùng cho các thao tác đọc hold.</param>
    public RedisHoldService(
        IRedisScriptExecutor scriptExecutor,
        string createHoldScript,
        string confirmHoldScript,
        string releaseHoldScript,
        IDatabase redisDatabase)
    {
        _scriptExecutor = scriptExecutor;
        _createHoldScript = createHoldScript;
        _confirmHoldScript = confirmHoldScript;
        _releaseHoldScript = releaseHoldScript;
        _redisDatabase = redisDatabase;
    }

    /// <summary>
    /// Tạo hold mới và reserve inventory trên Redis bằng Lua script atomic.
    /// Kiểm tra tồn kho, kiểm tra 1 user/1 hold active, trừ inventory khả dụng,
    /// tạo <c>hold:{holdId}</c> (TTL 60s), set <c>hold:user:{clientId}</c> và thêm <c>holds:expiring</c>.
    /// </summary>
    /// <param name="productId">Mã sản phẩm.</param>
    /// <param name="quantity">Số lượng cần giữ chỗ.</param>
    /// <param name="clientId">Định danh client/user sở hữu hold.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả tạo hold (thành công, hết hàng, hoặc đã có hold active).</returns>
    public async Task<HoldCreateResult> CreateHoldAsync(string productId, int quantity, string clientId, CancellationToken cancellationToken = default)
    {
        var holdId = Guid.NewGuid();
        var ttlSeconds = DefaultTtlSeconds;
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);
        var expiresAtEpochMs = expiresAt.ToUnixTimeMilliseconds();

        var result = await _scriptExecutor.EvaluateAsync(
            _createHoldScript,
            new RedisKey[]
            {
                $"inventory:{productId}",
                $"hold:{holdId}",
                $"hold:user:{clientId}",
                "holds:expiring"
            },
            new RedisValue[]
            {
                quantity,
                clientId,
                holdId.ToString(),
                ttlSeconds,
                expiresAtEpochMs,
                productId
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Hold create script trả về kết quả không hợp lệ.");

        var parts = result.Split(',', StringSplitOptions.None);
        return parts[0] switch
        {
            "OK" => new HoldCreateResult(
                HoldCreationStatus.Success,
                Guid.Parse(parts[1]),
                DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(parts[2])),
                int.Parse(parts[3])),
            "INSUFFICIENT" => new HoldCreateResult(
                HoldCreationStatus.InsufficientInventory,
                AvailableQuantity: parts.Length > 1 && int.TryParse(parts[1], out var available) ? available : 0),
            "ALREADY_EXISTS" => new HoldCreateResult(
                HoldCreationStatus.ActiveHoldAlreadyExists,
                ExistingHoldId: parts.Length > 1 && Guid.TryParse(parts[1], out var existing) ? existing : null),
            _ => throw new InvalidOperationException($"Hold create script trả về trạng thái không hỗ trợ: {result}")
        };
    }

    /// <summary>
    /// Consume/confirm hold trên Redis bằng Lua script atomic.
    /// Chỉ thành công khi hold tồn tại, đang active, đúng owner, chưa hết hạn và chưa bị consume.
    /// </summary>
    /// <param name="holdId">Id của hold cần confirm.</param>
    /// <param name="clientId">Định danh client/user thực hiện confirm.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Kết quả consume kèm dữ liệu hold khi thành công.</returns>
    public async Task<HoldConsumeResult> ConsumeHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default)
    {
        var result = await _scriptExecutor.EvaluateAsync(
            _confirmHoldScript,
            new RedisKey[]
            {
                $"hold:{holdId}",
                $"hold:user:{clientId}",
                "holds:expiring"
            },
            new RedisValue[]
            {
                holdId.ToString(),
                clientId,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            return new HoldConsumeResult(HoldConsumeStatus.Invalid);

        var parts = result.Split(',', StringSplitOptions.None);
        return parts[0] switch
        {
            "OK" => new HoldConsumeResult(
                HoldConsumeStatus.Success,
                new Hold(
                    holdId,
                    parts.Length > 1 ? parts[1] : string.Empty,
                    parts.Length > 2 && int.TryParse(parts[2], out var qty) ? qty : 0,
                    parts.Length > 3 ? parts[3] : clientId,
                    parts.Length > 4 && long.TryParse(parts[4], out var expMs)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(expMs)
                        : DateTimeOffset.UtcNow)),
            "NOT_FOUND" => new HoldConsumeResult(HoldConsumeStatus.NotFound),
            "EXPIRED" => new HoldConsumeResult(HoldConsumeStatus.Expired),
            "CONSUMED" => new HoldConsumeResult(HoldConsumeStatus.Consumed),
            "NOT_OWNED" => new HoldConsumeResult(HoldConsumeStatus.NotOwned),
            "INVALID" => new HoldConsumeResult(HoldConsumeStatus.Invalid),
            _ => new HoldConsumeResult(HoldConsumeStatus.Invalid)
        };
    }

    /// <summary>
    /// Release/hủy hold và trả inventory về Redis bằng Lua script (idempotent).
    /// Nếu hold đang active sẽ trả slot, cập nhật trạng thái và dọn index/expiring set.
    /// </summary>
    /// <param name="holdId">Id của hold cần release.</param>
    /// <param name="clientId">Định danh client/user yêu cầu release (kiểm tra quyền sở hữu ở mode user).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Trạng thái release (released/already released/expired released/not found/consumed/invalid).</returns>
    public async Task<HoldReleaseResult> ReleaseHoldAsync(Guid holdId, string clientId, CancellationToken cancellationToken = default)
    {
        var result = await _scriptExecutor.EvaluateAsync(
            _releaseHoldScript,
            new RedisKey[]
            {
                $"hold:{holdId}",
                $"hold:user:{clientId}",
                "holds:expiring"
            },
            new RedisValue[]
            {
                holdId.ToString(),
                clientId ?? string.Empty,
                "user"
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            return new HoldReleaseResult(HoldReleaseStatus.Invalid);

        return result switch
        {
            "RELEASED" => new HoldReleaseResult(HoldReleaseStatus.Released),
            "ALREADY_RELEASED" => new HoldReleaseResult(HoldReleaseStatus.AlreadyReleased),
            "EXPIRED_RELEASED" => new HoldReleaseResult(HoldReleaseStatus.ExpiredReleased),
            "NOT_FOUND" => new HoldReleaseResult(HoldReleaseStatus.NotFound),
            "NOT_OWNED" => new HoldReleaseResult(HoldReleaseStatus.NotOwned),
            "CONSUMED" => new HoldReleaseResult(HoldReleaseStatus.Consumed),
            "INVALID" => new HoldReleaseResult(HoldReleaseStatus.Invalid),
            _ => new HoldReleaseResult(HoldReleaseStatus.Invalid)
        };
    }

    /// <summary>
    /// Lấy thông tin hold từ Redis hash <c>hold:{holdId}</c>.
    /// </summary>
    /// <param name="holdId">Id của hold cần đọc.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Đối tượng <see cref="Hold"/> nếu tồn tại và đọc được dữ liệu hợp lệ; ngược lại null.</returns>
    public async Task<Hold?> GetByIdAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var entries = await _redisDatabase.HashGetAllAsync($"hold:{holdId}");
        if (entries.Length == 0)
            return null;

        var map = entries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
        if (!map.TryGetValue("productId", out var productId) ||
            !map.TryGetValue("quantity", out var quantityStr) ||
            !map.TryGetValue("clientId", out var clientId) ||
            !map.TryGetValue("expiresAt", out var expiresAtStr))
        {
            return null;
        }

        _ = int.TryParse(quantityStr, out var quantity);
        _ = long.TryParse(expiresAtStr, out var expiresAtMs);
        var hold = new Hold(holdId, productId, quantity, clientId, DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs));

        if (map.TryGetValue("status", out var status))
        {
            switch (status)
            {
                case "consumed":
                    hold.MarkConsumed();
                    break;
                case "released":
                    hold.MarkReleased();
                    break;
                case "expired":
                    hold.MarkExpired();
                    break;
            }
        }

        return hold;
    }

    /// <summary>
    /// Lấy các HoldId đã hết hạn từ ZSET <c>holds:expiring</c> (score &lt;= now).
    /// </summary>
    /// <param name="now">Mốc thời gian hiện tại.</param>
    /// <param name="limit">Số lượng tối đa.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Danh sách HoldId hết hạn.</returns>
    public Task<IReadOnlyList<Guid>> GetExpiredHoldIdsAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken = default)
    {
        var values = _redisDatabase.SortedSetRangeByScore(
            "holds:expiring",
            double.NegativeInfinity,
            now.ToUnixTimeMilliseconds(),
            Exclude.None,
            StackExchange.Redis.Order.Ascending,
            0,
            limit);

        var ids = values
            .Select(v => Guid.TryParse(v.ToString(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        return Task.FromResult<IReadOnlyList<Guid>>(ids);
    }

    /// <summary>
    /// Release hold hết hạn ở chế độ hệ thống (mode expire), idempotent, không yêu cầu ownership.
    /// </summary>
    /// <param name="holdId">Id hold cần release do hết hạn.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Trạng thái release.</returns>
    public async Task<HoldReleaseResult> ReleaseExpiredHoldAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var result = await _scriptExecutor.EvaluateAsync(
            _releaseHoldScript,
            new RedisKey[]
            {
                $"hold:{holdId}",
                $"hold:user:", // placeholder; script resolves owner key from hold data
                "holds:expiring"
            },
            new RedisValue[]
            {
                holdId.ToString(),
                string.Empty,
                "expire"
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            return new HoldReleaseResult(HoldReleaseStatus.Invalid);

        return result switch
        {
            "RELEASED" => new HoldReleaseResult(HoldReleaseStatus.Released),
            "ALREADY_RELEASED" => new HoldReleaseResult(HoldReleaseStatus.AlreadyReleased),
            "EXPIRED_RELEASED" => new HoldReleaseResult(HoldReleaseStatus.ExpiredReleased),
            "NOT_FOUND" => new HoldReleaseResult(HoldReleaseStatus.NotFound),
            "NOT_OWNED" => new HoldReleaseResult(HoldReleaseStatus.NotOwned),
            "CONSUMED" => new HoldReleaseResult(HoldReleaseStatus.Consumed),
            "INVALID" => new HoldReleaseResult(HoldReleaseStatus.Invalid),
            _ => new HoldReleaseResult(HoldReleaseStatus.Invalid)
        };
    }
}
