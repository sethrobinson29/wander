using Wander.Api.Domain;

namespace Wander.Tests.Helpers;

internal static class CardBuilder
{
    public static Card Card(
        string name = "Test Card",
        string typeLine = "Creature — Human",
        string? oracleText = null,
        List<string>? colorIdentity = null,
        Dictionary<string, string>? legalities = null) => new()
        {
            Id = Guid.NewGuid(),
            ScryfallId = Guid.NewGuid().ToString(),
            Name = name,
            TypeLine = typeLine,
            OracleText = oracleText,
            ColorIdentity = colorIdentity ?? [],
            Legalities = legalities ?? [],
        };

    public static Card BasicLand(string name = "Plains") => Card(
        name: name,
        typeLine: "Basic Land — Plains");

    public static Card WithPartner(string name = "Test Partner") => Card(
        name: name,
        typeLine: "Legendary Creature — Human",
        oracleText: "Partner");

    public static Card WithPartnerWith(string name, string partnerName) => Card(
        name: name,
        typeLine: "Legendary Creature — Human",
        oracleText: $"Partner with {partnerName}");

    public static Card AnyNumberAllowed(string name = "Relentless Rats") => Card(
        name: name,
        typeLine: "Creature — Rat",
        oracleText: "A deck can have any number of cards named Relentless Rats.");

    public static DeckCard DeckCard(
        Card card,
        int quantity = 1,
        bool isCommander = false,
        bool isSideboard = false) => new()
        {
            Id = Guid.NewGuid(),
            DeckId = Guid.NewGuid(),
            CardId = card.Id,
            Card = card,
            Quantity = quantity,
            IsCommander = isCommander,
            IsSideboard = isSideboard,
        };

    public static DeckCard Commander(Card card) =>
        DeckCard(card, quantity: 1, isCommander: true);
}