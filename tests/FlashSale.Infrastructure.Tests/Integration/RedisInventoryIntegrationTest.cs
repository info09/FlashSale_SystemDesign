using System.IO;
using FluentAssertions;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class RedisInventoryIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer _redisConnection = null!;
    private IDatabase _redisDatabase = null!;
    private IRedisScriptExecutor _scriptExecutor = null!;
    private IInventoryRepository _inventoryRepository = null!;
    private readonly string _luaScriptContent;

    public RedisInventoryIntegrationTest()
    {
        // Doc Lua Script atomic tu embedded resource (giong production code trong DependencyInjection)
        var assembly = typeof(RedisInventoryService).Assembly;
        var resourceName = "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.atomic_inventory_deduct.lua";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Khong tim thay embedded resource '{resourceName}'");
        using var reader = new StreamReader(stream);
        _luaScriptContent = reader.ReadToEnd();

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
        _inventoryRepository = new RedisInventoryService(_scriptExecutor, _luaScriptContent);
    }

    public async Task DisposeAsync()
    {
        await _redisConnection.DisposeAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenEnoughInventory_ShouldReturnRemainingQuantity()
    {
        // Arrange
        var productId = "product-1";
        var initialQuantity = 100;
        var quantityToDeduct = 10;
        await _inventoryRepository.SetInventoryAsync(productId, initialQuantity);

        // Act
        var result = await _inventoryRepository.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.Should().Be(initialQuantity - quantityToDeduct);
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenNotEnoughInventory_ShouldReturnNull()
    {
        // Arrange
        var productId = "product-2";
        var initialQuantity = 5;
        var quantityToDeduct = 100;
        await _inventoryRepository.SetInventoryAsync(productId, initialQuantity);

        // Act
        var result = await _inventoryRepository.DeductInventoryAsync(productId, quantityToDeduct);

        // Assert
        result.Should().BeNull(); // OverSelling - khong du kho
    }

    [Fact]
    public async Task DeductInventoryAsync_WhenConcurrentDeduct_ShouldNotOversell()
    {
        // Arrange
        var productId = "product-3";
        var initialQuantity = 1;
        await _inventoryRepository.SetInventoryAsync(productId, initialQuantity);

        // Act: Nhieu request deduct cung luc (simulate race condition)
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _inventoryRepository.DeductInventoryAsync(productId, 1))
            .ToList();
        var results = await Task.WhenAll(tasks);

        // Assert: Chi duy nhat 1 request duoc phep tru kho (atomic, khong over-selling)
        var successCount = results.Count(r => r.HasValue);
        successCount.Should().Be(1);
    }

    [Fact]
    public async Task GetInventoryAsync_AfterDeduct_ShouldReturnRemainingQuantity()
    {
        // Arrange
        var productId = "product-4";
        var initialQuantity = 50;
        await _inventoryRepository.SetInventoryAsync(productId, initialQuantity);

        // Act
        await _inventoryRepository.DeductInventoryAsync(productId, 20);
        var result = await _inventoryRepository.GetInventoryAsync(productId);

        // Assert
        result.Should().Be(initialQuantity - 20);
    }
}
