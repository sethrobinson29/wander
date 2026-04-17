using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Wander.Api.Domain;

namespace Wander.Api.Infrastructure.Data;

public class WanderDbContext : IdentityDbContext<ApplicationUser>
{
    public WanderDbContext(DbContextOptions<WanderDbContext> options) : base(options) { }

    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardPrinting> CardPrintings => Set<CardPrinting>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckCard> DeckCards => Set<DeckCard>();
    public DbSet<UserFollow> Follows => Set<UserFollow>();
    public DbSet<DeckLike> DeckLikes => Set<DeckLike>();
    public DbSet<DeckComment> DeckComments => Set<DeckComment>();

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

            entity.HasMany(c => c.Printings)
                  .WithOne(p => p.Card)
                  .HasForeignKey(p => p.CardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardPrinting>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.ScryfallId).IsUnique();
            entity.HasIndex(p => p.CardId);
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

            entity.HasOne(d => d.CoverPrinting)
                    .WithMany()
                    .HasForeignKey(d => d.CoverPrintingId)
                    .OnDelete(DeleteBehavior.SetNull);
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
            entity.HasOne(dc => dc.Printing)
                  .WithMany()
                  .HasForeignKey(dc => dc.PrintingId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.HasKey(f => new { f.FollowerId, f.FolloweeId });
            entity.HasOne(f => f.Follower)
                  .WithMany(u => u.Following)
                  .HasForeignKey(f => f.FollowerId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(f => f.Followee)
                  .WithMany(u => u.Followers)
                  .HasForeignKey(f => f.FolloweeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DeckLike>().HasKey(l => new { l.UserId, l.DeckId });

        modelBuilder.Entity<DeckComment>()
                .ToTable("DeckComment")
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
    }
}
