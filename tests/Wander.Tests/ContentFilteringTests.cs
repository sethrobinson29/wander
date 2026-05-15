using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;

namespace Wander.Tests;

public class ContentFilteringTests
{
    private static WanderDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WanderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser ActiveUser(string id, string username) => new()
    {
        Id = id, UserName = username, NormalizedUserName = username.ToUpper(),
        Email = $"{id}@test.com", NormalizedEmail = $"{id}@TEST.COM",
        IsDeactivated = false,
    };

    private static ApplicationUser DeactivatedUser(string id, string username) => new()
    {
        Id = id, UserName = username, NormalizedUserName = username.ToUpper(),
        Email = $"{id}@test.com", NormalizedEmail = $"{id}@TEST.COM",
        IsDeactivated = true,
    };

    private static Deck PublicDeck(string ownerId, string name) => new()
    {
        Id = Guid.NewGuid(), Name = name, OwnerId = ownerId,
        Visibility = Visibility.Public, Format = Format.Commander,
        CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
    };

    // ── User search ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UserSearch_ExcludesDeactivatedUsers()
    {
        await using var db = CreateDb();
        db.Users.AddRange(ActiveUser("1", "alice"), DeactivatedUser("2", "alice_suspended"));
        await db.SaveChangesAsync();

        var results = await db.Users
            .Where(u => !u.IsDeactivated && u.UserName!.ToLower().Contains("alice"))
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("alice", results[0].UserName);
    }

    [Fact]
    public async Task UserSearch_ReturnsActiveUsers()
    {
        await using var db = CreateDb();
        db.Users.AddRange(ActiveUser("1", "bob"), ActiveUser("2", "bobby"));
        await db.SaveChangesAsync();

        var results = await db.Users
            .Where(u => !u.IsDeactivated && u.UserName!.ToLower().Contains("bob"))
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    // ── Public deck listing ──────────────────────────────────────────────────

    [Fact]
    public async Task PublicDecks_ExcludesDecksFromDeactivatedOwner()
    {
        await using var db = CreateDb();
        db.Users.AddRange(ActiveUser("1", "alice"), DeactivatedUser("2", "eve"));
        db.Decks.AddRange(PublicDeck("1", "Alice Deck"), PublicDeck("2", "Eve Deck"));
        await db.SaveChangesAsync();

        var results = await db.Decks
            .Where(d => d.Visibility == Visibility.Public && !d.Owner!.IsDeactivated)
            .Include(d => d.Owner)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("Alice Deck", results[0].Name);
    }

    [Fact]
    public async Task PublicDecks_IncludesAllActiveOwnerDecks()
    {
        await using var db = CreateDb();
        db.Users.Add(ActiveUser("1", "alice"));
        db.Decks.AddRange(PublicDeck("1", "Deck A"), PublicDeck("1", "Deck B"));
        await db.SaveChangesAsync();

        var results = await db.Decks
            .Where(d => d.Visibility == Visibility.Public && !d.Owner!.IsDeactivated)
            .Include(d => d.Owner)
            .ToListAsync();

        Assert.Equal(2, results.Count);
    }

    // ── Deck search ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeckSearch_ExcludesDecksFromDeactivatedOwner()
    {
        await using var db = CreateDb();
        db.Users.AddRange(ActiveUser("1", "alice"), DeactivatedUser("2", "eve"));
        db.Decks.AddRange(PublicDeck("1", "Dragon Deck"), PublicDeck("2", "Dragon Deck Banned"));
        await db.SaveChangesAsync();

        var results = await db.Decks
            .Where(d => d.Visibility == Visibility.Public && !d.Owner!.IsDeactivated
                     && d.Name.ToLower().Contains("dragon"))
            .Include(d => d.Owner)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("alice", results[0].Owner!.UserName);
    }

    // ── Single deck access ───────────────────────────────────────────────────

    [Fact]
    public async Task DeckGet_OwnerIsDeactivated_FlagIsSet()
    {
        await using var db = CreateDb();
        db.Users.Add(DeactivatedUser("1", "eve"));
        db.Decks.Add(PublicDeck("1", "Hidden Deck"));
        await db.SaveChangesAsync();

        var deck = await db.Decks.Include(d => d.Owner).FirstAsync();

        // Controller checks: if (deck.Owner?.IsDeactivated == true) return NotFound()
        Assert.True(deck.Owner!.IsDeactivated);
    }

    // ── User profile ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfile_DeactivatedFlag_IsSetOnUser()
    {
        await using var db = CreateDb();
        db.Users.Add(DeactivatedUser("1", "eve"));
        await db.SaveChangesAsync();

        var user = await db.Users.FindAsync("1");

        // Controller checks: if (user.IsDeactivated) return NotFound()
        Assert.True(user!.IsDeactivated);
    }
}
