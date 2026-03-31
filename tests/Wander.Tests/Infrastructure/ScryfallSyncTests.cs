using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Infrastructure.Scryfall;

namespace Wander.Tests.Infrastructure;

public class ScryfallSyncTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncAsync_PopulatesCards()
    {
        var options = new DbContextOptionsBuilder<WanderDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        await using var db = new WanderDbContext(options);

        var httpClient = new HttpClient();
        var logger = NullLogger<ScryfallBulkDataService>.Instance;
        var service = new ScryfallBulkDataService(httpClient, db, logger);

        await service.SyncAsync();

        var count = await db.Cards.CountAsync();
        Assert.True(count > 20_000, $"Expected 20k+ cards, got {count}");
    }
}