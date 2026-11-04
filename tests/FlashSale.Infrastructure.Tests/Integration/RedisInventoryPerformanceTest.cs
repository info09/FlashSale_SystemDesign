using System.Diagnostics;
using System.IO;
using FluentAssertions;
using FlashSale.Domain.Interfaces;
using FlashSale.Infrastructure.ExternalServices.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class RedisInventoryPerformanceTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer _redisConnection = null!;
    private IDatabase _redisDatabase = null!;
    private IRedisScriptExecutor _scriptExecutor = null!;
    private IInventoryRepository _inventoryRepository = null!;
    private readonly string _luaScriptContent;
    private const int IterationCount = 1000;

    public RedisInventoryPerformanceTest()
    {
        // Doc Lua Script atomic tu embedded resource (giong production code)
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
    public async Task DeductInventoryAsync_P99Latency_ShouldBeUnder20ms()
    {
        // Arrange
        var productId = "perf-product-1";
        await _inventoryRepository.SetInventoryAsync(productId, 100000);

        // Warmup de khoi tao ket noi Redis, JIT, v.v.
        for (int i = 0; i < 10; i++)
        {
            await _inventoryRepository.DeductInventoryAsync(productId, 1);
        }

        // Act: Doc p99 latency qua 1000 lan deduct
        var latencies = new List<long>(IterationCount);
        for (int i = 0; i < IterationCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await _inventoryRepository.DeductInventoryAsync(productId, 1);
            sw.Stop();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        // Tinh p99 latency
        latencies.Sort();
        var p99Index = (int)Math.Ceiling(IterationCount * 0.99) - 1;
        var p99Latency = latencies[p99Index];
        var avgLatency = latencies.Average();
        var maxLatency = latencies.Max();

        // In ra ket qua de theo doi (khong la production code)
        Console.WriteLine($"AtomicInventoryOffloading P99 latency: {p99Latency}ms (Avg: {avgLatency}ms, Max: {maxLatency}ms, Iterations: {IterationCount})");

        // Assert: NFR-01 p99 latency fast-path < 20ms
        p99Latency.Should().BeLessThan(20);
    }
}
