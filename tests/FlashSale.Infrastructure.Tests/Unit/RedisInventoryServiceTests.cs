using FluentAssertions;
using FlashSale.Infrastructure.ExternalServices.Redis;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Unit;

public class RedisInventoryServiceTests
{
    private readonly Mock<IRedisScriptExecutor> _scriptExecutorMock;
    private readonly string _luaScriptContent;
    private readonly RedisInventoryService _service;

    public RedisInventoryServiceTests()
    {
        _scriptExecutorMock = new Mock<IRedisScriptExecutor>();
        _luaScriptContent = @"-- Lua Script: Atomic Inventory Offloading (nguyen tu, khong chia thanh nhieu lenh)
-- KEYS[1]: key luu so luong ton kho trong Redis (vi du: inventory:{productId})
-- ARGV[1]: quantityToDeduct (so luong can tru)

local key = KEYS[1]
local quantityToDeduct = tonumber(ARGV[1])

-- Lay so luong ton kho hien tai
local current = tonumber(redis.call(""GET"", key) or ""0"")

-- Neu khong du kho, tra ve nil (signal Co thue)
if current < quantityToDeduct then
    return nil
end

-- Tru kho atomic
local newQuantity = current - quantityToDeduct
redis.call(""SET"", key, newQuantity)

-- Tra ve so luong con lai
return newQuantity";
        _service = new RedisInventoryService(_scriptExecutorMock.Object, _luaScriptContent);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenEnoughInventory_ShouldReturnRemainingQuantity()
    {
        // Arrange
        var productId = "product-1";
        var quantityToDeduct = 10;
        var remainingQuantity = 90;
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(remainingQuantity.ToString());

        // Act
        var result = await _service.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.Should().Be(remainingQuantity);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenNotEnoughInventory_ShouldReturnNull()
    {
        // Arrange
        var productId = "product-1";
        var quantityToDeduct = 100;
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInventoryAsync_WhenKeyExists_ShouldReturnQuantity()
    {
        // Arrange
        var productId = "product-1";
        var currentQuantity = 50;
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentQuantity.ToString());

        // Act
        var result = await _service.GetInventoryAsync(productId);

        // Assert
        result.Should().Be(currentQuantity);
    }

    [Fact]
    public async Task GetInventoryAsync_WhenKeyNotExists_ShouldReturnNull()
    {
        // Arrange
        var productId = "product-1";
        _scriptExecutorMock
            .Setup(x => x.EvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetInventoryAsync(productId);

        // Assert
        result.Should().BeNull();
    }
}
