using System.IO;
using FlashSale.Domain.Interfaces;
using FlashSale.Domain.Models;
using FlashSale.Infrastructure.ExternalServices.Redis;
using FluentAssertions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace FlashSale.Infrastructure.Tests.Integration;

public class RedisHoldIntegrationTest : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private IConnectionMultiplexer _redisConnection = null!;
    private IDatabase _redisDatabase = null!;
    private IRedisScriptExecutor _scriptExecutor = null!;
    private IInventoryRepository _inventoryRepository = null!;
    private IHoldRepository _holdRepository = null!;

    public RedisHoldIntegrationTest()
    {
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

        var infraAssembly = typeof(RedisHoldService).Assembly;
        _inventoryRepository = new RedisInventoryService(_scriptExecutor, ReadResource(infraAssembly, "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.atomic_inventory_deduct.lua"), _redisDatabase);
        _holdRepository = new RedisHoldService(
            _scriptExecutor,
            ReadResource(infraAssembly, "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_create.lua"),
            ReadResource(infraAssembly, "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_confirm.lua"),
            ReadResource(infraAssembly, "FlashSale.Infrastructure.ExternalServices.Redis.Scripts.hold_release.lua"),
            _redisDatabase);
    }

    public async Task DisposeAsync()
    {
        await _redisConnection.DisposeAsync();
        await _redisContainer.StopAsync();
        await _redisContainer.DisposeAsync();
    }

    [Fact]
    public async Task CreateHoldAsync_WhenEnoughInventory_ShouldReserveAndCreateHold()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 10);

        var result = await _holdRepository.CreateHoldAsync("p1", 2, "c1");

        result.Status.Should().Be(HoldCreationStatus.Success);
        result.HoldId.Should().NotBeNull();
        result.ExpiresAt.Should().NotBeNull();
        result.TtlSeconds.Should().Be(60);

        var remaining = await _inventoryRepository.GetInventoryAsync("p1");
        remaining.Should().Be(8);

        (await _redisDatabase.KeyExistsAsync($"hold:{result.HoldId}")).Should().BeTrue();
        (await _redisDatabase.StringGetAsync("hold:user:c1")).ToString().Should().Be(result.HoldId.ToString());
        (await _redisDatabase.SortedSetScoreAsync("holds:expiring", result.HoldId.ToString())).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateHoldAsync_WhenInsufficientInventory_ShouldReturnInsufficientWithoutCreatingHold()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 1);

        var result = await _holdRepository.CreateHoldAsync("p1", 2, "c1");

        result.Status.Should().Be(HoldCreationStatus.InsufficientInventory);
        result.HoldId.Should().BeNull();
        (await _inventoryRepository.GetInventoryAsync("p1")).Should().Be(1);
        (await _redisDatabase.KeyExistsAsync("hold:user:c1")).Should().BeFalse();
    }

    [Fact]
    public async Task CreateHoldAsync_WhenUserHasActiveHold_ShouldReturnAlreadyExists()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 10);
        var first = await _holdRepository.CreateHoldAsync("p1", 1, "c1");

        var second = await _holdRepository.CreateHoldAsync("p1", 1, "c1");

        second.Status.Should().Be(HoldCreationStatus.ActiveHoldAlreadyExists);
        second.ExistingHoldId.Should().Be(first.HoldId);
        (await _inventoryRepository.GetInventoryAsync("p1")).Should().Be(9);
    }

    [Fact]
    public async Task ConsumeHoldAsync_WhenValid_ShouldSucceedAndReturnHoldData()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 5);
        var created = await _holdRepository.CreateHoldAsync("p1", 2, "c1");

        var consumed = await _holdRepository.ConsumeHoldAsync(created.HoldId!.Value, "c1");

        consumed.Status.Should().Be(HoldConsumeStatus.Success);
        consumed.Hold.Should().NotBeNull();
        consumed.Hold!.Id.Should().Be(created.HoldId!.Value);
        consumed.Hold.ProductId.Should().Be("p1");
        consumed.Hold.Quantity.Should().Be(2);
        consumed.Hold.ClientId.Should().Be("c1");

        (await _redisDatabase.StringGetAsync("hold:user:c1")).HasValue.Should().BeFalse();
        (await _redisDatabase.SortedSetScoreAsync("holds:expiring", created.HoldId.ToString())).Should().BeNull();
    }

    [Fact]
    public async Task ConsumeHoldAsync_WhenNotOwned_ShouldReturnNotOwned()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 5);
        var created = await _holdRepository.CreateHoldAsync("p1", 1, "c1");

        var consumed = await _holdRepository.ConsumeHoldAsync(created.HoldId!.Value, "c2");

        consumed.Status.Should().Be(HoldConsumeStatus.NotOwned);
    }

    [Fact]
    public async Task ReleaseHoldAsync_WhenActive_ShouldReturnInventoryAndBeIdempotent()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 5);
        var created = await _holdRepository.CreateHoldAsync("p1", 2, "c1");

        var released = await _holdRepository.ReleaseHoldAsync(created.HoldId!.Value, "c1");
        var releasedAgain = await _holdRepository.ReleaseHoldAsync(created.HoldId!.Value, "c1");

        released.Status.Should().Be(HoldReleaseStatus.Released);
        releasedAgain.Status.Should().BeOneOf(HoldReleaseStatus.AlreadyReleased, HoldReleaseStatus.ExpiredReleased, HoldReleaseStatus.Released);
        (await _inventoryRepository.GetInventoryAsync("p1")).Should().Be(5);
        (await _redisDatabase.KeyExistsAsync("hold:user:c1")).Should().BeFalse();
        (await _redisDatabase.SortedSetScoreAsync("holds:expiring", created.HoldId.ToString())).Should().BeNull();
    }

    [Fact]
    public async Task ConsumeHoldAsync_WhenRaceDoubleConsume_ShouldAllowOnlyOneSuccess()
    {
        await _inventoryRepository.SetInventoryAsync("p1", 5);
        var created = await _holdRepository.CreateHoldAsync("p1", 1, "c1");

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _holdRepository.ConsumeHoldAsync(created.HoldId!.Value, "c1"))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        results.Count(r => r.Status == HoldConsumeStatus.Success).Should().Be(1);
        results.Count(r => r.Status != HoldConsumeStatus.Success).Should().Be(9);
    }

    private static string ReadResource(System.Reflection.Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Khong tim thay embedded resource '{resourceName}'");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
