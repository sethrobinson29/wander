using Microsoft.EntityFrameworkCore;
using Wander.Api.Infrastructure.Data;

namespace Wander.Tests.Infrastructure;

[Trait("Category", "Integration")]
public class DbConnectionTests
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev";

    [Fact]
    public async Task WanderDbContext_CanConnectToPostgres()
    {
        var options = new DbContextOptionsBuilder<WanderDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;

        await using var context = new WanderDbContext(options);

        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(canConnect, "Could not connect to local PostgreSQL. Is Docker running?");
    }
}