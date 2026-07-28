using Wander.Client.Models;
using Wander.Client.Services;

namespace Wander.Client.Tests.Services;

public class PlaytestStateServiceTests
{
    static DeckCardDetail MakeCard(
        string name, int quantity = 1, bool isCommander = false, bool isSideboard = false,
        string? backImageUriNormal = null) => new(
        Id: Guid.NewGuid(), CardId: Guid.NewGuid(), PrintingId: null, CardName: name,
        ManaCost: null, Cmc: 1, TypeLine: "Creature", OracleText: null, FlavorText: null,
        Legalities: [], ColorIdentity: [], ImageUriNormal: $"https://example.com/{name}.jpg",
        ImageUriSmall: null, ImageUriArtCrop: null, ImagePrintingId: null,
        Quantity: quantity, IsCommander: isCommander, IsSideboard: isSideboard, Errors: [],
        BackFaceManaCost: null, BackFaceTypeLine: null, BackFaceOracleText: null,
        BackImageUriNormal: backImageUriNormal);

    static DeckDetail MakeDeck(Format format, params DeckCardDetail[] cards) => new(
        Id: Guid.NewGuid(), Name: "Test Deck", Description: null, Primer: null,
        Format: format, CoverImageUri: null, CoverCropLeft: null, CoverCropTop: null,
        CoverCropWidth: null, CoverCropHeight: null, Visibility: Visibility.Private,
        OwnerId: "owner", OwnerUsername: "owner", Cards: cards.ToList(), DeckErrors: [],
        LikeCount: 0, IsLikedByCurrentUser: false,
        CreatedAt: DateTimeOffset.UtcNow, UpdatedAt: DateTimeOffset.UtcNow);

    static DeckDetail CommanderDeck(int nonCommanderCount = 14, int sideboardCount = 0)
    {
        var cards = new List<DeckCardDetail> { MakeCard("Atraxa", isCommander: true) };
        for (var i = 0; i < nonCommanderCount; i++)
            cards.Add(MakeCard($"Card {i}"));
        for (var i = 0; i < sideboardCount; i++)
            cards.Add(MakeCard($"Sideboard {i}", isSideboard: true));
        return MakeDeck(Format.Commander, cards.ToArray());
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Load_CommanderFormat_StartsAt40LifeAndTurn1()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        Assert.Equal(40, service.State!.Life);
        Assert.Equal(1, service.State.TurnNumber);
    }

    [Theory]
    [InlineData(Format.Standard)]
    [InlineData(Format.Modern)]
    [InlineData(Format.Legacy)]
    public void Load_NonCommanderFormat_StartsAt20Life(Format format)
    {
        var service = new PlaytestStateService();
        service.Load(MakeDeck(format, MakeCard("Card 0"), MakeCard("Card 1"), MakeCard("Card 2"),
            MakeCard("Card 3"), MakeCard("Card 4"), MakeCard("Card 5"), MakeCard("Card 6"), MakeCard("Card 7")));

        Assert.Equal(20, service.State!.Life);
    }

    [Fact]
    public void Load_Commander_StartsInCommandZone_NotShuffledOrDrawn()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        var commander = service.State!.Cards.Single(c => c.Name == "Atraxa");
        Assert.Equal(PlaytestZone.Command, commander.Zone);
        Assert.DoesNotContain(commander.InstanceId, service.State.LibraryOrder);
    }

    [Fact]
    public void Load_SideboardCards_AreExcluded()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck(sideboardCount: 3));

        Assert.DoesNotContain(service.State!.Cards, c => c.Name.StartsWith("Sideboard"));
    }

    [Fact]
    public void Load_QuantityExpandsIntoIndividualInstances()
    {
        var service = new PlaytestStateService();
        service.Load(MakeDeck(Format.Commander, MakeCard("Sol Ring", quantity: 4)));

        var instances = service.State!.Cards.Where(c => c.Name == "Sol Ring").ToList();
        Assert.Equal(4, instances.Count);
        Assert.Equal(4, instances.Select(c => c.InstanceId).Distinct().Count());
    }

    [Fact]
    public void Load_DealsOpeningHandOfSeven()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        Assert.Equal(7, service.State!.Cards.Count(c => c.Zone == PlaytestZone.Hand));
        Assert.Equal(14 - 7, service.State.LibraryOrder.Count);
    }

    // ── Shuffle ───────────────────────────────────────────────────────────────

    [Fact]
    public void Shuffle_PreservesLibraryContents()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var before = service.State!.LibraryOrder.ToHashSet();

        service.Shuffle();

        Assert.Equal(before, service.State.LibraryOrder.ToHashSet());
    }

    // ── DrawCard ──────────────────────────────────────────────────────────────

    [Fact]
    public void DrawCard_MovesTopOfLibraryToHand()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var topId = service.State!.LibraryOrder[0];
        var libraryCountBefore = service.State.LibraryOrder.Count;

        service.DrawCard();

        var top = service.State.Cards.Single(c => c.InstanceId == topId);
        Assert.Equal(PlaytestZone.Hand, top.Zone);
        Assert.Equal(libraryCountBefore - 1, service.State.LibraryOrder.Count);
    }

    [Fact]
    public void DrawCard_EmptyLibrary_IsNoOp()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck(nonCommanderCount: 7));
        Assert.Empty(service.State!.LibraryOrder);

        var exception = Record.Exception(() => service.DrawCard());

        Assert.Null(exception);
        Assert.Empty(service.State.LibraryOrder);
    }

    // ── NextTurn ──────────────────────────────────────────────────────────────

    [Fact]
    public void NextTurn_IncrementsTurnAndDraws()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var handCountBefore = service.State!.Cards.Count(c => c.Zone == PlaytestZone.Hand);

        service.NextTurn();

        Assert.Equal(2, service.State.TurnNumber);
        Assert.Equal(handCountBefore + 1, service.State.Cards.Count(c => c.Zone == PlaytestZone.Hand));
    }

    [Fact]
    public void NextTurn_UntapsTappedCards_ExceptSkipUntap()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var (a, b) = PlaceTwoOnBattlefield(service);
        service.ToggleTap(a.InstanceId);
        service.ToggleTap(b.InstanceId);
        service.ToggleSkipUntap(b.InstanceId);

        service.NextTurn();

        Assert.False(service.State!.Cards.Single(c => c.InstanceId == a.InstanceId).Tapped);
        Assert.True(service.State.Cards.Single(c => c.InstanceId == b.InstanceId).Tapped);
    }

    static (GameCardInstance, GameCardInstance) PlaceTwoOnBattlefield(PlaytestStateService service)
    {
        var hand = service.State!.Cards.Where(c => c.Zone == PlaytestZone.Hand).Take(2).ToList();
        service.SetBattlefieldPosition(hand[0].InstanceId, 50, 50);
        service.SetBattlefieldPosition(hand[1].InstanceId, 100, 100);
        return (hand[0], hand[1]);
    }

    // ── MoveCard ──────────────────────────────────────────────────────────────

    [Fact]
    public void MoveCard_OutOfLibrary_RemovesFromLibraryOrder()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var id = service.State!.LibraryOrder[0];

        service.MoveCard(id, PlaytestZone.Graveyard);

        Assert.DoesNotContain(id, service.State.LibraryOrder);
        Assert.Equal(PlaytestZone.Graveyard, service.State.Cards.Single(c => c.InstanceId == id).Zone);
    }

    [Fact]
    public void MoveCard_IntoLibrary_InsertsAtTop()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var handCard = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);

        service.MoveCard(handCard.InstanceId, PlaytestZone.Library);

        Assert.Equal(handCard.InstanceId, service.State.LibraryOrder[0]);
    }

    [Fact]
    public void MoveCard_ClearsTappedAndBattlefieldPosition()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var handCard = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);
        service.SetBattlefieldPosition(handCard.InstanceId, 50, 50);
        service.ToggleTap(handCard.InstanceId);

        service.MoveCard(handCard.InstanceId, PlaytestZone.Graveyard);

        var moved = service.State.Cards.Single(c => c.InstanceId == handCard.InstanceId);
        Assert.False(moved.Tapped);
        Assert.Null(moved.PositionX);
        Assert.Null(moved.PositionY);
    }

    // ── SetBattlefieldPosition ────────────────────────────────────────────────

    [Fact]
    public void SetBattlefieldPosition_RepositioningOnBattlefield_DoesNotResetTapped()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var handCard = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);
        service.SetBattlefieldPosition(handCard.InstanceId, 50, 50);
        service.ToggleTap(handCard.InstanceId);

        service.SetBattlefieldPosition(handCard.InstanceId, 200, 200);

        Assert.True(service.State.Cards.Single(c => c.InstanceId == handCard.InstanceId).Tapped);
    }

    // ── Toggle / Flip / Counter ───────────────────────────────────────────────

    [Fact]
    public void ToggleTap_Flips()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var card = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);

        service.ToggleTap(card.InstanceId);
        Assert.True(service.State.Cards.Single(c => c.InstanceId == card.InstanceId).Tapped);

        service.ToggleTap(card.InstanceId);
        Assert.False(service.State.Cards.Single(c => c.InstanceId == card.InstanceId).Tapped);
    }

    [Fact]
    public void Flip_WorksOnNonDfcCards()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var card = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);
        Assert.False(card.IsDfc);

        service.Flip(card.InstanceId);

        Assert.True(service.State.Cards.Single(c => c.InstanceId == card.InstanceId).ShowingBack);
    }

    [Fact]
    public void AdjustCounter_AllowsNegative()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var card = service.State!.Cards.First(c => c.Zone == PlaytestZone.Hand);

        service.AdjustCounter(card.InstanceId, -3);

        Assert.Equal(-3, service.State.Cards.Single(c => c.InstanceId == card.InstanceId).Counter);
    }

    // ── Life ──────────────────────────────────────────────────────────────────

    [Fact]
    public void AdjustLife_AllowsNegative()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        service.AdjustLife(-100);

        Assert.Equal(-60, service.State!.Life);
    }

    // ── Mana ──────────────────────────────────────────────────────────────────

    [Fact]
    public void AdjustMana_ClampsAtZero()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        service.AdjustMana("R", -1);

        Assert.Equal(0, service.State!.ManaPool["R"]);
    }

    [Fact]
    public void AdjustMana_UnknownColor_IsNoOp()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());

        var exception = Record.Exception(() => service.AdjustMana("X", 1));

        Assert.Null(exception);
    }

    [Fact]
    public void ResetMana_ZeroesAllColors()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        foreach (var color in new[] { "W", "U", "B", "R", "G", "C" })
            service.AdjustMana(color, 2);

        service.ResetMana();

        Assert.All(service.State!.ManaPool.Values, v => Assert.Equal(0, v));
    }

    // ── Tokens ────────────────────────────────────────────────────────────────

    [Fact]
    public void CreateToken_AddsBattlefieldToken()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var countBefore = service.State!.Cards.Count;

        service.CreateToken("Zombie", "2/2", "B");

        var token = service.State.Cards.Single(c => c.Name == "Zombie");
        Assert.Equal(countBefore + 1, service.State.Cards.Count);
        Assert.True(token.IsToken);
        Assert.Equal(PlaytestZone.Battlefield, token.Zone);
        Assert.Equal("2/2", token.TokenPowerToughness);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_RestoresDefaultsAndFreshHand()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        service.AdjustLife(-10);
        service.NextTurn();
        service.AdjustMana("W", 2);

        service.Reset();

        Assert.Equal(40, service.State!.Life);
        Assert.Equal(1, service.State.TurnNumber);
        Assert.All(service.State.ManaPool.Values, v => Assert.Equal(0, v));
        Assert.Equal(7, service.State.Cards.Count(c => c.Zone == PlaytestZone.Hand));
    }

    // ── OnChange ──────────────────────────────────────────────────────────────

    [Fact]
    public void OnChange_FiresOnMutation()
    {
        var service = new PlaytestStateService();
        service.Load(CommanderDeck());
        var notified = false;
        service.OnChange += () => notified = true;

        service.DrawCard();

        Assert.True(notified);
    }
}
