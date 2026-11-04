using System.IO;
using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class RedisRateLimitIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer _redisConnection = null!;
    private IDatabase _redisDatabase = null!;
    private IRedisScriptExecutor _scriptExecutor = null!;
    private RedisRateLimitService _service = null!;
    private readonly string _luaScriptContent;

    public RedisRateLimitIntegrationTest()
    {
        _luaScriptContent = @"-- Lua Script: Token Bucket Rate Limit (nguyen tu, khong chia thanh nhieu lenh)
-- KEYS[1]: key luu trang thai token bucket (vi du: rate_limit:{clientId})
-- ARGV[1]: maxRequests (so luong toi da trong 1 cua so)
-- ARGV[2]: windowSeconds (khoang thoi gian reset, giay)
-- ARGV[3]: now (timestamp Unix epoch, giay)

local key = KEYS[1]
local maxRequests = tonumber(ARGV[1])
local windowSeconds = tonumber(ARGV[2])
local now = tonumber(ARGV[3])

-- Lay trang thai hien tu tu Redis (format: tokens:last_refill)
local current = redis.call(""GET"", key)
local tokens = maxRequests
local lastRefill = now

if current then
    local parts = {}
    for part in string.gmatch(current, ""([^:]+)"") do
        table.insert(parts, part)
    end
    tokens = tonumber(parts[1]) or maxRequests
    lastRefill = tonumber(parts[2]) or now
end

-- Tinh toan so token da nap them tu lan cuoi cung den hien tai
local elapsed = now - lastRefill
if elapsed > 0 then
    local refill = math.floor(elapsed * maxRequests / windowSeconds)
    if refill > 0 then
        tokens = math.min(maxRequests, tokens + refill)
        lastRefill = now
    end
end

local allowed = 0
local resetAfter = windowSeconds

if tokens >= 1 then
    -- Co token, tieu 1 token
    tokens = tokens - 1
    allowed = 1
    resetAfter = 0
else
    -- Khong con token, tinh thoi gian cho den khi co token moi
    local elapsedSinceLastRefill = now - lastRefill
    resetAfter = math.max(0, windowSeconds - (elapsedSinceLastRefill % windowSeconds))
end

-- Luu trang thai moi vao Redis (format: tokens:last_refill)
redis.call(""SET"", key, tokens .. "":"" .. lastRefill)
redis.call(""EXPIRE"", key, windowSeconds * 2)

return { allowed, tokens, resetAfter }";
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString());
        _redisDatabase = _redisConnection.GetDatabase();
        _scriptExecutor = new RedisScriptExecutor(_redisDatabase);
        _service = new RedisRateLimitService(_scriptExecutor, _luaScriptContent);
    }

    public async Task DisposeAsync()
    {
        await _redisConnection.DisposeAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task CheckAndConsumeAsync_WhenAllowed_ShouldReturnRateLimitAllowed()
    {
        // Arrange
        var clientId = "client-1";
        var maxRequests = 100;
        var windowSeconds = 60;

        // Act
        var result = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(maxRequests - 1);
        result.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task CheckAndConsumeAsync_WhenExhausted_ShouldReturnRateLimitRejected()
    {
        // Arrange
        var clientId = "client-2";
        var maxRequests = 2;
        var windowSeconds = 60;

        // Act: Hết 2 lượt đầu tiên
        var first = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);
        var second = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);
        var third = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);

        // Assert
        first.IsAllowed.Should().BeTrue();
        second.IsAllowed.Should().BeTrue();
        third.IsAllowed.Should().BeFalse();
        third.RemainingRequests.Should().Be(0);
        third.ResetAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckAndConsumeAsync_DifferentClients_ShouldHaveIndependentLimits()
    {
        // Arrange
        var maxRequests = 5;
        var windowSeconds = 60;

        // Act
        var client1 = await _service.CheckAndConsumeAsync("client-a", maxRequests, windowSeconds);
        var client2 = await _service.CheckAndConsumeAsync("client-b", maxRequests, windowSeconds);

        // Assert
        client1.IsAllowed.Should().BeTrue();
        client2.IsAllowed.Should().BeTrue();
        client1.RemainingRequests.Should().Be(maxRequests - 1);
        client2.RemainingRequests.Should().Be(maxRequests - 1);
    }
}
