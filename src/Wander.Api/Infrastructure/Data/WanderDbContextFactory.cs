using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContextFactory : IDesignTimeDbContextFactory<WanderDbContext>
{
    public WanderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WanderDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=wander;Username=wander;Password=wander_dev")
            .Options;

        return new WanderDbContext(options);
    }
}
