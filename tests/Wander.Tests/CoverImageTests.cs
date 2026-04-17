using Wander.Api.Domain;
using Wander.Api.Services;
using Wander.Tests.Helpers;

namespace Wander.Tests;

public class CoverImageTests
{
    private static Deck Deck(List<DeckCard> cards, CardPrinting? coverPrinting = null) => new()
    {
        Name = "Test Deck",
        OwnerId = "user1",
        Cards = cards,
        CoverPrinting = coverPrinting,
    };

    [Fact]
    public void ExplicitCoverPrinting_ReturnsCoverArtCrop()
    {
        var card = CardBuilder.Card();
        var printing = CardBuilder.Printing(card, artCrop: "https://example.com/cover.jpg");
        var deck = Deck([], coverPrinting: printing);

        Assert.Equal("https://example.com/cover.jpg", DeckDisplayService.ResolveCoverImage(deck));
    }

    [Fact]
    public void ExplicitCoverPrinting_NullArtCrop_ReturnsNull()
    {
        var card = CardBuilder.Card();
        var printing = CardBuilder.Printing(card, artCrop: null);
        var deck = Deck([], coverPrinting: printing);

        Assert.Null(DeckDisplayService.ResolveCoverImage(deck));
    }

    [Fact]
    public void NoCover_CommanderHasExplicitPrinting_ReturnsCommanderArtCrop()
    {
        var card = CardBuilder.Card();
        var printing = CardBuilder.Printing(card, artCrop: "https://example.com/commander.jpg");
        var commander = CardBuilder.Commander(card);
        commander.Printing = printing;
        var deck = Deck([commander]);

        Assert.Equal("https://example.com/commander.jpg", DeckDisplayService.ResolveCoverImage(deck));
    }

    [Fact]
    public void NoCover_CommanderHasCardPrintings_ReturnsFirstCardPrinting()
    {
        var card = CardBuilder.Card();
        var printing = CardBuilder.Printing(card, artCrop: "https://example.com/default.jpg");
        card.Printings.Add(printing);
        var commander = CardBuilder.Commander(card);
        var deck = Deck([commander]);

        Assert.Equal("https://example.com/default.jpg", DeckDisplayService.ResolveCoverImage(deck));
    }

    [Fact]
    public void NoCover_NoCommander_ReturnsNull()
    {
        var card = CardBuilder.Card();
        var deck = Deck([CardBuilder.DeckCard(card)]);

        Assert.Null(DeckDisplayService.ResolveCoverImage(deck));
    }

    [Fact]
    public void ExplicitCoverPrinting_TakesPrecedenceOverCommander()
    {
        var card = CardBuilder.Card();
        var commanderPrinting = CardBuilder.Printing(card, artCrop: "https://example.com/commander.jpg");
        var coverPrinting = CardBuilder.Printing(card, artCrop: "https://example.com/override.jpg");
        var commander = CardBuilder.Commander(card);
        commander.Printing = commanderPrinting;
        var deck = Deck([commander], coverPrinting: coverPrinting);

        Assert.Equal("https://example.com/override.jpg", DeckDisplayService.ResolveCoverImage(deck));
    }
}
