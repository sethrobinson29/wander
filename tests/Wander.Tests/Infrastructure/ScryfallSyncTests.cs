using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Infrastructure.Scryfall;
using Wander.Api.Services;
using Wander.Tests.Helpers;

namespace Wander.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class ScryfallSyncTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev";

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "ScryfallSync")]
    public async Task SyncAsync_PopulatesCards()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(TestConnectionString);
        dataSourceBuilder.EnableDynamicJson();

        var options = new DbContextOptionsBuilder<WanderDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        await using var db = new WanderDbContext(options);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Wander/1.0 (mtg-deck-manager)");
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.Timeout = TimeSpan.FromMinutes(10);
        var logger = NullLogger<ScryfallBulkDataService>.Instance;
        var auditLog = NullAuditLogService.Instance;
        var service = new ScryfallBulkDataService(httpClient, db, logger, auditLog);

        await service.SyncAsync();

        var count = await db.Cards.CountAsync();
        Assert.True(count > 20_000, $"Expected 20k+ cards, got {count}");
    }
}