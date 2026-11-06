using FlashSale.Application.Interfaces;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests.EndToEnd;

public class OutboxPurgeApiEndToEndTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public OutboxPurgeApiEndToEndTest()
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
    public async Task PurgeEndpoint_ShouldDeleteOldProcessedOutboxesAndReturnDeletedCount()
    {
        // Arrange: seed Outbox cu (Published/Failed, cu 31 ngay) va moi (Pending, 1 ngay)
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            await context.Database.MigrateAsync();
            await context.Outboxes.ExecuteDeleteAsync();

            var olderThan30 = DateTimeOffset.UtcNow.AddDays(-31);
            context.Outboxes.AddRange(
                new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", olderThan30.AddDays(-1), Guid.NewGuid(), "Published", 0, DateTimeOffset.UtcNow.AddDays(-1)),
                new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", olderThan30.AddDays(-2), Guid.NewGuid(), "Failed", 0, DateTimeOffset.UtcNow.AddDays(-2)),
                new Outbox(Guid.NewGuid(), Guid.NewGuid(), "OrderCreated", "{}", DateTimeOffset.UtcNow.AddDays(-1), Guid.NewGuid(), "Pending", 0, null));
            await context.SaveChangesAsync();
        }

        var token = await RegisterAndGetToken(_factory, $"purgeapi_{Guid.NewGuid():N}", $"purgeapi_{Guid.NewGuid():N}@example.com", "Password123!");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {token}");

        // Act: goi endpoint POST /api/v1/outbox/purge
        var response = await client.PostAsync("/api/v1/outbox/purge", null);

        // Assert: tra ve 200 va deletedCount = 2
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("deletedCount").GetInt32().Should().Be(2);

        // Assert: chi con lai 1 ghi Pending moi
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PostgreSQLPrimaryContext>();
            var remaining = await context.Outboxes.CountAsync();
            remaining.Should().Be(1);
            remaining.Should().Be(await context.Outboxes.CountAsync(o => o.ProcessingStatus == "Pending"));
        }
    }

    private static async Task<string> RegisterAndGetToken(WebApplicationFactory<Program> factory, string userName, string email, string password)
    {
        using var client = factory.CreateClient();

        var registerRequest = new { UserName = userName, Email = email, Password = password };
        var registerContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
        var registerResponse = await client.PostAsync("/api/v1/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginRequest = new { UserName = userName, Password = password };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        return loginDoc.RootElement.GetProperty("accessToken").GetString()!;
    }
}
