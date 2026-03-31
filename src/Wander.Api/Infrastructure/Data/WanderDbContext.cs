using Microsoft.EntityFrameworkCore;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContext : DbContext
{
    public WanderDbContext(DbContextOptions<WanderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Entity configurations added per phase
    }
}
