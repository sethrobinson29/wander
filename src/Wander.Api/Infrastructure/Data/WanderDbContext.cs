using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Wander.Api.Domain;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContext : IdentityDbContext<ApplicationUser>
{
    public WanderDbContext(DbContextOptions<WanderDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckCard> DeckCards => Set<DeckCard>();

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

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Token).IsUnique();
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Deck>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.Owner)
                  .WithMany(u => u.Decks)
                  .HasForeignKey(d => d.OwnerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeckCard>(entity =>
        {
            entity.HasKey(dc => dc.Id);
            entity.HasOne(dc => dc.Deck)
                  .WithMany(d => d.Cards)
                  .HasForeignKey(dc => dc.DeckId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(dc => dc.Card)
                  .WithMany()
                  .HasForeignKey(dc => dc.CardId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}