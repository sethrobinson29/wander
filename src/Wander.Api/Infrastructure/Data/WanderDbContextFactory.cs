using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContextFactory : IDesignTimeDbContextFactory<WanderDbContext>
{
    public WanderDbContext CreateDbContext(string[] args)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(
            "Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev");
        dataSourceBuilder.EnableDynamicJson();

        var options = new DbContextOptionsBuilder<WanderDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        return new WanderDbContext(options);
    }
}
