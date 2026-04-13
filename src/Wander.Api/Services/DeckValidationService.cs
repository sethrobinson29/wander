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

    private static readonly Dictionary<string, int> NamedCopyLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Seven Dwarves"] = 7,
        ["The Nazgûl"]    = 9,
    };

    // MinDeck is a minimum for non-exact formats, or the required exact total for Commander.
    private record FormatRules(int MinDeck, int MaxSideboard, bool Exact = false);

    private static readonly Dictionary<Format, FormatRules> DeckRules = new()
    {
        [Format.Standard]  = new(60, 15),
        [Format.Pioneer]   = new(60, 15),
        [Format.Modern]    = new(60, 15),
        [Format.Legacy]    = new(60, 15),
        [Format.Vintage]   = new(60, 15),
        [Format.Pauper]    = new(60, 15),
        [Format.Explorer]  = new(60, 15),
        [Format.Historic]  = new(60, 15),
        [Format.Timeless]  = new(60, 15),
        [Format.Commander] = new(100, 0, Exact: true),
    };

    // Deck-level structural errors that don't belong to any single card.
    public List<string> GetStructuralErrors(List<DeckCard> cards, Format format)
    {
        var errors = new List<string>();

        if (format == Format.Commander)
        {
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
        }

        if (DeckRules.TryGetValue(format, out var rules))
        {
            var mainCount = cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity);
            var sideCount = cards.Where(c => c.IsSideboard).Sum(c => c.Quantity);

            if (rules.Exact)
            {
                if (mainCount != rules.MinDeck)
                    errors.Add($"{format} decks must have exactly {rules.MinDeck} cards (currently {mainCount}).");
            }
            else
            {
                if (mainCount < rules.MinDeck)
                    errors.Add($"{format} decks must have at least {rules.MinDeck} cards in the main deck (currently {mainCount}).");
            }

            if (sideCount > rules.MaxSideboard)
                errors.Add(rules.MaxSideboard == 0
                    ? $"{format} decks do not use a sideboard."
                    : $"Sideboard cannot exceed {rules.MaxSideboard} cards (currently {sideCount}).");
        }

        return errors;
    }

    // Per-card errors for a single card in the context of its deck.
    // Errors are returned without the card name — callers attach them directly to the card.
    public List<string> GetCardErrors(DeckCard dc, Format format, HashSet<string> commanderColorIdentity)
    {
        var errors = new List<string>();

        if (format == Format.Commander && !dc.IsCommander)
        {
            var limit = GetCardCopyLimit(dc.Card, isCommander: true);
            if (dc.Quantity > limit)
                errors.Add(limit == 1
                    ? "Commander decks are singleton (max 1 copy, except basic lands and cards allowing any number)."
                    : $"Maximum {limit} copies allowed in Commander.");

            if (commanderColorIdentity.Count > 0)
            {
                var cardColorIdentity = dc.Card.ColorIdentity.ToHashSet();
                if (!cardColorIdentity.IsSubsetOf(commanderColorIdentity))
                    errors.Add($"Color identity {{{string.Join("", cardColorIdentity)}}} is outside the commander's color identity {{{string.Join("", commanderColorIdentity)}}}.");
            }
        }

        if (!dc.IsCommander && ScryfallFormatKeys.TryGetValue(format, out var scryfallFormat))
        {
            if (dc.Card.Legalities.TryGetValue(scryfallFormat, out var legality))
            {
                if (legality != "legal" && legality != "restricted")
                {
                    errors.Add($"Not legal in {format} ({legality}).");
                }
                else
                {
                    var maxCopies = legality == "restricted" ? 1 : GetCardCopyLimit(dc.Card, isCommander: false);
                    if (dc.Quantity > maxCopies)
                        errors.Add($"Maximum {maxCopies} {(maxCopies == 1 ? "copy" : "copies")} allowed in {format}{(legality == "restricted" ? " (restricted)" : "")}.");
                }
            }
        }

        return errors;
    }

    public static HashSet<string> GetCommanderColorIdentity(List<DeckCard> cards) =>
        cards.Where(c => c.IsCommander)
             .SelectMany(c => c.Card.ColorIdentity)
             .Distinct()
             .ToHashSet();

    // Returns the maximum number of copies allowed for a card.
    // int.MaxValue means unlimited (basic lands, "A deck can have any number" cards).
    // isCommander: true uses a default of 1; false uses a default of 4.
    private static int GetCardCopyLimit(Card card, bool isCommander)
    {
        if (BasicLandTypes.Any(t =>
            card.TypeLine.Contains(t, StringComparison.OrdinalIgnoreCase) &&
            card.TypeLine.Contains("Basic", StringComparison.OrdinalIgnoreCase)))
            return int.MaxValue;

        if (NamedCopyLimits.TryGetValue(card.Name, out var namedLimit))
            return namedLimit;

        if (card.OracleText != null &&
            card.OracleText.Contains("A deck can have any number", StringComparison.OrdinalIgnoreCase))
            return int.MaxValue;

        return isCommander ? 1 : 4;
    }
}
