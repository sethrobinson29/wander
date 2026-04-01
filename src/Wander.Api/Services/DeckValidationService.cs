using Wander.Api.Domain;

namespace Wander.Api.Services;

public class DeckValidationService
{
    private static readonly string[] BasicLandTypes = ["Plains", "Island", "Swamp", "Mountain", "Forest"];

    private static readonly Dictionary<Format, string> ScryfallFormatKeys = new()
    {
        [Format.Commander] = "commander",
        [Format.Standard]  = "standard",
        [Format.Pioneer]   = "pioneer",
        [Format.Modern]    = "modern",
        [Format.Legacy]    = "legacy",
        [Format.Vintage]   = "vintage",
        [Format.Pauper]    = "pauper",
        [Format.Explorer]  = "explorer",
        [Format.Historic]  = "historic",
        [Format.Timeless]  = "timeless",
    };

    // Cards with a named copy limit higher than 1 but less than unlimited
    private static readonly Dictionary<string, int> NamedCopyLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Seven Dwarves"] = 7,
        ["The Nazgûl"]    = 9,
    };

    public List<string> Validate(Deck deck, List<DeckCard> cards)
    {
        var errors = new List<string>();

        if (deck.Format == Format.Commander)
            errors.AddRange(ValidateCommander(cards));

        errors.AddRange(ValidateLegality(deck.Format, cards));

        return errors;
    }

    private static List<string> ValidateCommander(List<DeckCard> cards)
    {
        var errors = new List<string>();
        var commanders = cards.Where(c => c.IsCommander).ToList();

        if (commanders.Count == 0)
            errors.Add("Commander deck must have at least one commander.");

        if (commanders.Count > 2)
            errors.Add("Commander deck cannot have more than two commanders.");

        if (commanders.Count == 2)
        {
            var bothHavePartner = commanders.All(c =>
                c.Card.OracleText != null &&
                (c.Card.OracleText.Contains("Partner with", StringComparison.OrdinalIgnoreCase) ||
                 c.Card.OracleText.Contains("\nPartner\n", StringComparison.OrdinalIgnoreCase) ||
                 c.Card.OracleText.EndsWith("\nPartner", StringComparison.OrdinalIgnoreCase) ||
                 c.Card.OracleText.EndsWith("Partner", StringComparison.OrdinalIgnoreCase)));

            if (!bothHavePartner)
                errors.Add("Both commanders must have the Partner keyword.");
        }

        var commanderColorIdentity = commanders
            .SelectMany(c => c.Card.ColorIdentity)
            .Distinct()
            .ToHashSet();

        foreach (var dc in cards.Where(c => !c.IsCommander))
        {
            var limit = GetCardCopyLimit(dc.Card, isCommander: true);
            if (dc.Quantity > limit)
                errors.Add(limit == 1
                    ? $"{dc.Card.Name}: Commander decks are singleton (max 1 copy, except basic lands and cards allowing any number)."
                    : $"{dc.Card.Name}: Maximum {limit} copies allowed in Commander.");

            var cardColorIdentity = dc.Card.ColorIdentity.ToHashSet();
            if (!cardColorIdentity.IsSubsetOf(commanderColorIdentity))
                errors.Add($"{dc.Card.Name}: Color identity {string.Join("", cardColorIdentity)} is outside the commander's color identity {string.Join("", commanderColorIdentity)}.");
        }

        return errors;
    }

    private static List<string> ValidateLegality(Format format, List<DeckCard> cards)
    {
        var errors = new List<string>();

        if (!ScryfallFormatKeys.TryGetValue(format, out var scryfallFormat))
            return errors;

        foreach (var dc in cards.Where(c => !c.IsCommander))
        {
            if (!dc.Card.Legalities.TryGetValue(scryfallFormat, out var legality))
                continue;

            if (legality != "legal" && legality != "restricted")
            {
                errors.Add($"{dc.Card.Name} is not legal in {format} ({legality}).");
                continue;
            }

            // Restricted cards are capped at 1 copy regardless of the card's intrinsic limit
            var maxCopies = legality == "restricted" ? 1 : GetCardCopyLimit(dc.Card, isCommander: false);
            if (dc.Quantity > maxCopies)
                errors.Add($"{dc.Card.Name}: Maximum {maxCopies} {(maxCopies == 1 ? "copy" : "copies")} allowed in {format}{(legality == "restricted" ? " (restricted)" : "")}.");
        }

        return errors;
    }

    // Returns the maximum number of copies allowed for a card.
    // int.MaxValue means unlimited (basic lands, "A deck can have any number" cards).
    // isCommander: true uses a default of 1; false uses a default of 4.
    private static int GetCardCopyLimit(Card card, bool isCommander)
    {
        // Basic lands are always unlimited
        if (BasicLandTypes.Any(t =>
            card.TypeLine.Contains(t, StringComparison.OrdinalIgnoreCase) &&
            card.TypeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase)))
            return int.MaxValue;

        // Named cards with a specific copy limit (e.g., Seven Dwarves = 7, The Nazgûl = 9)
        if (NamedCopyLimits.TryGetValue(card.Name, out var namedLimit))
            return namedLimit;

        // Cards whose oracle text explicitly allows any number of copies
        if (card.OracleText != null &&
            card.OracleText.Contains("A deck can have any number", StringComparison.OrdinalIgnoreCase))
            return int.MaxValue;

        return isCommander ? 1 : 4;
    }
}
