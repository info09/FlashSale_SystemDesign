using FluentAssertions;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class RedisRateLimitServiceTests
{
    private readonly Mock<IRedisScriptExecutor> _scriptExecutorMock;
    private readonly string _luaScriptContent;
    private readonly RedisRateLimitService _service;

    public RedisRateLimitServiceTests()
    {
        _scriptExecutorMock = new Mock<IRedisScriptExecutor>();
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
        _service = new RedisRateLimitService(_scriptExecutorMock.Object, _luaScriptContent);
    }

    [Fact]
    public async Task CheckAndConsumeAsync_WhenAllowed_ShouldReturnRateLimitAllowed()
    {
        // Arrange
        var clientId = "client-1";
        var maxRequests = 100;
        var windowSeconds = 60;
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1,99,0");

        // Act
        var result = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RemainingRequests.Should().Be(99);
        result.ClientId.Should().Be(clientId);
    }

    [Fact]
    public async Task CheckAndConsumeAsync_WhenRejected_ShouldReturnRateLimitRejected()
    {
        // Arrange
        var clientId = "client-1";
        var maxRequests = 100;
        var windowSeconds = 60;
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("0,0,30");

        // Act
        var result = await _service.CheckAndConsumeAsync(clientId, maxRequests, windowSeconds);

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RemainingRequests.Should().Be(0);
        result.ClientId.Should().Be(clientId);
        result.ResetAfterSeconds.Should().Be(30);
    }
}
