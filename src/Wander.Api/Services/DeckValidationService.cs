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

    // Deck-level structural errors that don't belong to any single card
    // (commander count, partner keyword requirement).
    public List<string> GetStructuralErrors(List<DeckCard> cards, Format format)
    {
        if (format != Format.Commander) return [];

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
