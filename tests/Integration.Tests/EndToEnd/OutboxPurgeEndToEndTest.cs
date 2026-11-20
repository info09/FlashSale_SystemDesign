using FlashSale.Application.Interfaces;
using FlashSale.Application.Services;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class OutboxPurgeEndToEndTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public OutboxPurgeEndToEndTest()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("flashsale")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(
                [
                    new KeyValuePair<string, string>("POSTGRESQL_PRIMARY_CONNECTION_STRING", _postgresContainer.GetConnectionString()),
                    new KeyValuePair<string, string>("Outbox:PurgeDays", "30"),
                    new KeyValuePair<string, string>("JWT_SECRET", "test_jwt_secret_key_at_least_32_characters_long_for_hmac_sha256"),
                    new KeyValuePair<string, string>("JWT_ISSUER", "FlashSale.Api"),
                    new KeyValuePair<string, string>("JWT_AUDIENCE", "FlashSale.Client"),
                    new KeyValuePair<string, string>("JWT_EXPIRATION_MINUTES", "60"),
                ]);
            });
        });
    }

    public async Task DisposeAsync()
    {
        _factory?.Dispose();
        await _postgresContainer.StopAsync();
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Purge_ThroughDiContainer_ShouldDeleteOldProcessedOutboxesAndKeepNewOnes()
    {
        // Arrange: seed Outbox cu (Published/Failed, cu hon 30 ngay) va moi (Pending, 1 ngay)
        Guid oldPublishedId;
        Guid oldFailedId;
        Guid newPendingId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            await context.Database.MigrateAsync();
            await context.Outboxes.ExecuteDeleteAsync(); // xoa sach de test tu dong lap

            var olderThan30 = DateTimeOffset.UtcNow.AddDays(-31);
            var oldPublished = new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", olderThan30.AddDays(-1), Guid.NewGuid(), "Published", 0, DateTimeOffset.UtcNow.AddDays(-1));
            var oldFailed = new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", olderThan30.AddDays(-2), Guid.NewGuid(), "Failed", 0, DateTimeOffset.UtcNow.AddDays(-2));
            var newPending = new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", DateTimeOffset.UtcNow.AddDays(-1), Guid.NewGuid(), "Pending", 0, null);

            oldPublishedId = oldPublished.Id;
            oldFailedId = oldFailed.Id;
            newPendingId = newPending.Id;

            context.Outboxes.AddRange(oldPublished, oldFailed, newPending);
            await context.SaveChangesAsync();

            // Loai bo ban ghi phat sinh ngoai seed (neu co) de dam bao tinh xac dinh trong moi truong e2e
            await context.Outboxes
                .Where(o => o.Id != oldPublishedId && o.Id != oldFailedId && o.Id != newPendingId)
                .ExecuteDeleteAsync();
        }

        // Act: goi purge service qua DI container thuc te
        int deleted;
        using (var scope = _factory.Services.CreateScope())
        {
            var purgeService = scope.ServiceProvider.GetRequiredService<IOutboxPurgeService>();
            deleted = await purgeService.PurgeProcessedOutboxesAsync();
        }

        // Assert: it nhat 2 ban ghi cu da xu ly duoc xoa; ban ghi moi Pending van con
        deleted.Should().BeGreaterThanOrEqualTo(2);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();

            (await context.Outboxes.AnyAsync(o => o.Id == oldPublishedId)).Should().BeFalse();
            (await context.Outboxes.AnyAsync(o => o.Id == oldFailedId)).Should().BeFalse();

            var pending = await context.Outboxes.SingleAsync(o => o.Id == newPendingId);
            pending.ProcessingStatus.Should().Be("Pending");
        }
    }
}
