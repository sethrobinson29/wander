using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContext : DbContext
{
    public WanderDbContext(DbContextOptions<WanderDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.ScryfallId).IsUnique();
            entity.HasIndex(c => c.Name);

            entity.Property(c => c.Colors)
                  .HasColumnType("text[]");

            entity.Property(c => c.ColorIdentity)
                  .HasColumnType("text[]");

            entity.Property(c => c.Legalities)
                  .HasColumnType("jsonb");

            entity.Property(c => c.NameSearchVector)
                  .HasComputedColumnSql("to_tsvector('english', \"Name\")", stored: true);

            entity.HasIndex(c => c.NameSearchVector)
                  .HasMethod("GIN");
        });
    }
}
