using Wander.Api.Domain;
using Wander.Api.Services;
using Wander.Tests.Helpers;

namespace Wander.Tests;

public class DeckValidationTests
{
    private readonly DeckValidationService _svc = new();

    // ── Structural errors ────────────────────────────────────────────────────

    [Fact]
    public void NoCommander_ReturnsError()
    {
        var cards = new List<DeckCard> { CardBuilder.DeckCard(CardBuilder.Card()) };
        var errors = _svc.GetStructuralErrors(cards, Format.Commander);
        Assert.Contains(errors, e => e.Contains("at least one commander"));
    }

    [Fact]
    public void ThreeCommanders_ReturnsError()
    {
        var cards = Enumerable.Range(0, 3)
            .Select(_ => CardBuilder.Commander(CardBuilder.Card()))
            .ToList();
        var errors = _svc.GetStructuralErrors(cards, Format.Commander);
        Assert.Contains(errors, e => e.Contains("more than two commanders"));
    }

    [Fact]
    public void TwoCommandersWithPartner_NoError()
    {
        var a    = CardBuilder.Commander(CardBuilder.WithPartner("Kydele"));
        var b    = CardBuilder.Commander(CardBuilder.WithPartner("Silas Renn"));
        var main = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 98); // 98 + 2 commanders = 100
        var errors = _svc.GetStructuralErrors([a, b, main], Format.Commander);
        Assert.Empty(errors);
    }

    [Fact]
    public void TwoCommandersWithoutPartner_ReturnsError()
    {
        var a = CardBuilder.Commander(CardBuilder.Card("Atraxa"));
        var b = CardBuilder.Commander(CardBuilder.Card("Breya"));
        var errors = _svc.GetStructuralErrors([a, b], Format.Commander);
        Assert.Contains(errors, e => e.Contains("Partner keyword"));
    }

    [Fact]
    public void NonCommanderFormat_NoStructuralErrors()
    {
        var cards = new List<DeckCard> { CardBuilder.DeckCard(CardBuilder.Card(), quantity: 60) };
        var errors = _svc.GetStructuralErrors(cards, Format.Standard);
        Assert.Empty(errors);
    }

    // ── Singleton enforcement ────────────────────────────────────────────────

    [Fact]
    public void TwoCopiesOfNonland_InCommander_ReturnsError()
    {
        var dc = CardBuilder.DeckCard(CardBuilder.Card("Sol Ring"), quantity: 2);
        var errors = _svc.GetCardErrors(dc, Format.Commander, []);
        Assert.Contains(errors, e => e.Contains("singleton"));
    }

    // name, typeLine, oracleText, quantity, expectError
    [Theory]
    [InlineData("Plains",         "Basic Land — Plains",  null,                                                         20, false)]  // basic land — unlimited
    [InlineData("Relentless Rats","Creature — Rat",       "A deck can have any number of cards named Relentless Rats.", 30, false)]  // any number — unlimited
    [InlineData("Seven Dwarves",  "Creature — Dwarf",     null,                                                          7, false)]  // named limit — at limit
    [InlineData("Seven Dwarves",  "Creature — Dwarf",     null,                                                          8, true)]   // named limit — over limit
    [InlineData("The Nazgûl",     "Creature — Wraith",    null,                                                          9, false)]  // named limit — at limit
    [InlineData("The Nazgûl",     "Creature — Wraith",    null,                                                         10, true)]   // named limit — over limit
    public void CommanderCopyLimits(string name, string typeLine, string? oracleText, int qty, bool expectError)
    {
        var card = CardBuilder.Card(name, typeLine, oracleText);
        var dc = CardBuilder.DeckCard(card, quantity: qty);
        var errors = _svc.GetCardErrors(dc, Format.Commander, []);
        if (expectError)
            Assert.Contains(errors, e => e.Contains("Maximum") || e.Contains("singleton"));
        else
            Assert.DoesNotContain(errors, e => e.Contains("Maximum") || e.Contains("singleton"));
    }

    // ── Deck size rules ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(Format.Standard)]
    [InlineData(Format.Modern)]
    [InlineData(Format.Pauper)]
    [InlineData(Format.Vintage)]
    public void MainDeck_RequiresMinimumSixtyCards(Format format)
    {
        var deck59 = new List<DeckCard> { CardBuilder.DeckCard(CardBuilder.Card(), quantity: 59) };
        var deck60 = new List<DeckCard> { CardBuilder.DeckCard(CardBuilder.Card(), quantity: 60) };

        Assert.Contains(_svc.GetStructuralErrors(deck59, format), e => e.Contains("at least 60"));
        Assert.DoesNotContain(_svc.GetStructuralErrors(deck60, format), e => e.Contains("at least 60"));
    }

    [Theory]
    [InlineData(Format.Standard)]
    [InlineData(Format.Modern)]
    [InlineData(Format.Pauper)]
    [InlineData(Format.Vintage)]
    public void Sideboard_MaximumFifteenCards(Format format)
    {
        var main  = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 60);
        var side15 = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 15, isSideboard: true);
        var side16 = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 16, isSideboard: true);

        Assert.DoesNotContain(_svc.GetStructuralErrors([main, side15], format), e => e.Contains("Sideboard"));
        Assert.Contains(_svc.GetStructuralErrors([main, side16], format), e => e.Contains("Sideboard"));
    }

    [Fact]
    public void Commander_RequiresExactlyOneHundredCards()
    {
        var commander = CardBuilder.Commander(CardBuilder.Card());
        var main98 = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 98); // 98 + 1 cmd = 99
        var main99 = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 99); // 99 + 1 cmd = 100

        Assert.Contains(_svc.GetStructuralErrors([commander, main98], Format.Commander), e => e.Contains("exactly 100"));
        Assert.DoesNotContain(_svc.GetStructuralErrors([commander, main99], Format.Commander), e => e.Contains("exactly 100"));
    }

    [Fact]
    public void Commander_NoSideboard()
    {
        var commander = CardBuilder.Commander(CardBuilder.Card());
        var main      = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 99);
        var side      = CardBuilder.DeckCard(CardBuilder.Card(), quantity: 1, isSideboard: true);

        var errors = _svc.GetStructuralErrors([commander, main, side], Format.Commander);
        Assert.Contains(errors, e => e.Contains("do not use a sideboard"));
    }

    // ── Color identity ───────────────────────────────────────────────────────

    [Fact]
    public void CardOutsideColorIdentity_ReturnsError()
    {
        var card = CardBuilder.Card(colorIdentity: ["U"]);  // Blue card
        var dc = CardBuilder.DeckCard(card);
        var commanderIdentity = new HashSet<string> { "W", "B" };  // Orzhov commander

        var errors = _svc.GetCardErrors(dc, Format.Commander, commanderIdentity);
        Assert.Contains(errors, e => e.Contains("outside the commander's color identity"));
    }

    [Fact]
    public void CardWithinColorIdentity_NoError()
    {
        var card = CardBuilder.Card(colorIdentity: ["W"]);
        var dc = CardBuilder.DeckCard(card);
        var commanderIdentity = new HashSet<string> { "W", "U" };

        var errors = _svc.GetCardErrors(dc, Format.Commander, commanderIdentity);
        Assert.DoesNotContain(errors, e => e.Contains("color identity"));
    }

    [Fact]
    public void ColorlessCard_FitsAnyColorIdentity()
    {
        var card = CardBuilder.Card(colorIdentity: []);  // Sol Ring, Wastes, etc.
        var dc = CardBuilder.DeckCard(card);
        var commanderIdentity = new HashSet<string> { "R" };

        var errors = _svc.GetCardErrors(dc, Format.Commander, commanderIdentity);
        Assert.DoesNotContain(errors, e => e.Contains("color identity"));
    }

    [Fact]
    public void EmptyCommanderIdentity_SkipsColorCheck()
    {
        // Before a commander is assigned, color identity is empty — skip the check
        // so every card doesn't get a spurious "outside identity" error.
        var card = CardBuilder.Card(colorIdentity: ["U", "R"]);
        var dc = CardBuilder.DeckCard(card);

        var errors = _svc.GetCardErrors(dc, Format.Commander, commanderColorIdentity: []);
        Assert.DoesNotContain(errors, e => e.Contains("color identity"));
    }

    // ── Format legality ──────────────────────────────────────────────────────

    [Fact]
    public void BannedCard_InStandard_ReturnsError()
    {
        var legalities = new Dictionary<string, string> { ["standard"] = "banned" };
        var card = CardBuilder.Card(legalities: legalities);
        var dc = CardBuilder.DeckCard(card);

        var errors = _svc.GetCardErrors(dc, Format.Standard, []);
        Assert.Contains(errors, e => e.Contains("Not legal in Standard"));
    }

    [Fact]
    public void RestrictedCard_AllowsOneCopy()
    {
        var legalities = new Dictionary<string, string> { ["vintage"] = "restricted" };
        var card = CardBuilder.Card(legalities: legalities);
        var dc1 = CardBuilder.DeckCard(card, quantity: 1);
        var dc2 = CardBuilder.DeckCard(card, quantity: 2);

        Assert.DoesNotContain(_svc.GetCardErrors(dc1, Format.Vintage, []), e => e.Contains("Maximum"));
        Assert.Contains(_svc.GetCardErrors(dc2, Format.Vintage, []), e => e.Contains("Maximum 1"));
    }

    [Fact]
    public void LegalCard_FourCopiesAllowed_InModern()
    {
        var legalities = new Dictionary<string, string> { ["modern"] = "legal" };
        var card = CardBuilder.Card(legalities: legalities);
        var dc4 = CardBuilder.DeckCard(card, quantity: 4);
        var dc5 = CardBuilder.DeckCard(card, quantity: 5);

        Assert.DoesNotContain(_svc.GetCardErrors(dc4, Format.Modern, []), e => e.Contains("Maximum"));
        Assert.Contains(_svc.GetCardErrors(dc5, Format.Modern, []), e => e.Contains("Maximum 4"));
    }

    // ── GetCommanderColorIdentity ────────────────────────────────────────────

    [Fact]
    public void GetCommanderColorIdentity_UnionsAllCommanders()
    {
        var c1 = CardBuilder.Commander(CardBuilder.Card(colorIdentity: ["W", "U"]));
        var c2 = CardBuilder.Commander(CardBuilder.Card(colorIdentity: ["U", "B"]));
        var nonCmd = CardBuilder.DeckCard(CardBuilder.Card(colorIdentity: ["R"]));

        var identity = DeckValidationService.GetCommanderColorIdentity([c1, c2, nonCmd]);
        Assert.Equal(new HashSet<string> { "W", "U", "B" }, identity);
    }
}