using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Decks;
using Wander.Api.Services;

namespace Wander.Api.Controllers;

[ApiController]
[Route("decks")]
public class DeckController(WanderDbContext db, DeckValidationService validator) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Queries (public or owner-filtered) ──────────────────────────────────

    [HttpGet("public")]
    public async Task<ActionResult<List<DeckSummaryResponse>>> ListPublic(
        [FromQuery] Format? format,
        CancellationToken ct)
    {
        IQueryable<Deck> query = db.Decks
            .Where(d => d.Visibility == Visibility.Public)
            .Include(d => d.Owner)
            .Include(d => d.Cards);

        if (format.HasValue)
            query = query.Where(d => d.Format == format.Value);

        var decks = await query.OrderByDescending(d => d.UpdatedAt).ToListAsync(ct);
        return Ok(decks.Select(ToSummary));
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<DeckSummaryResponse>>> ListMine(CancellationToken ct)
    {
        var decks = await db.Decks
            .Where(d => d.OwnerId == UserId)
            .Include(d => d.Owner)
            .Include(d => d.Cards)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);


        return Ok(decks.Select(ToSummary));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeckDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();

        // Private decks visible only to their owner
        if (deck.Visibility == Visibility.Private)
        {
            var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (requesterId != deck.OwnerId) return NotFound();
        }

        return Ok(ToDetail(deck));
    }

    // ── Mutations ────────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<DeckDetailResponse>> Create(
        CreateDeckRequest request,
        CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Format)) return BadRequest(new { error = "Invalid format." });
        if (!Enum.IsDefined(request.Visibility)) return BadRequest(new { error = "Invalid visibility." });

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Format = request.Format,
            Visibility = request.Visibility,
            OwnerId = UserId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        db.Decks.Add(deck);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = deck.Id }, ToDetail(deck));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<DeckDetailResponse>> Update(
        Guid id,
        UpdateDeckRequest request,
        CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        if (!Enum.IsDefined(request.Format)) return BadRequest(new { error = "Invalid format." });
        if (!Enum.IsDefined(request.Visibility)) return BadRequest(new { error = "Invalid visibility." });

        var primerError = MarkdownValidator.ValidatePrimer(request.Primer);
        if (primerError != null) return BadRequest(new { error = primerError });

        deck.Name = request.Name;
        deck.Description = request.Description;
        deck.Primer = request.Primer;
        deck.Format = request.Format;
        deck.Visibility = request.Visibility;
        deck.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToDetail(deck));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deck = await db.Decks.FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        db.Decks.Remove(deck);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Card management ──────────────────────────────────────────────────────

    [HttpPut("{id:guid}/cards")]
    [Authorize]
    public async Task<ActionResult<DeckDetailResponse>> SetCards(
        Guid id,
        List<DeckCardRequest> request,
        CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        // Resolve all card IDs upfront to avoid N+1
        var cardIds = request.Select(r => r.CardId).ToList();
        var cards = await db.Cards
            .Where(c => cardIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        var missing = cardIds.Except(cards.Keys).ToList();
        if (missing.Count > 0)
            return BadRequest(new { errors = missing.Select(id => $"Card {id} not found.") });

        // Validate any specified printings exist and belong to the right card
        var printingIds = request.Where(r => r.PrintingId.HasValue).Select(r => r.PrintingId!.Value).Distinct().ToList();
        var printings = printingIds.Count > 0
            ? await db.CardPrintings.Where(p => printingIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct)
            : [];

        var badPrintings = request
            .Where(r => r.PrintingId.HasValue && (!printings.TryGetValue(r.PrintingId.Value, out var p) || p.CardId != r.CardId))
            .Select(r => $"Printing {r.PrintingId} not found for card {r.CardId}.")
            .ToList();
        if (badPrintings.Count > 0)
            return BadRequest(new { errors = badPrintings });

        // Replace all cards (simplest correct approach for a PUT)
        db.DeckCards.RemoveRange(deck.Cards);

        var newCards = request.Select(r => new DeckCard
        {
            Id = Guid.NewGuid(),
            DeckId = deck.Id,
            CardId = r.CardId,
            Card = cards[r.CardId],
            PrintingId = r.PrintingId,
            Quantity = r.Quantity,
            IsCommander = r.IsCommander,
            IsSideboard = r.IsSideboard,
        }).ToList();

        db.DeckCards.AddRange(newCards);
        deck.Cards = newCards;
        deck.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(ToDetail(deck));
    }

    // ── Bulk import ──────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/import")]
    [Authorize]
    public async Task<ActionResult<DeckDetailResponse>> BulkImport(
        Guid id,
        [FromBody] BulkImportRequest request,
        CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        var parsed = ParseDecklist(request.Decklist);

        var cardNames = parsed.Select(p => p.Name).Distinct().ToList();
        // Double-faced cards are stored as "Front // Back" in the DB.
        // Build a lookup that matches both the full name and the front face name.
        var dbCards = await db.Cards
            .Where(c => cardNames.Contains(c.Name) ||
                        cardNames.Any(n => c.Name.StartsWith(n + " //")))
            .ToListAsync(ct);

        var cards = new Dictionary<string, Domain.Card>(StringComparer.OrdinalIgnoreCase);
        foreach (var card in dbCards)
        {
            // Index by full name
            cards.TryAdd(card.Name, card);
            // Also index by front face name for DFCs (e.g. "Barkchannel Pathway")
            var slashIndex = card.Name.IndexOf(" //", StringComparison.Ordinal);
            if (slashIndex > 0)
                cards.TryAdd(card.Name[..slashIndex], card);
        }

        var notFound = cardNames.Where(n => !cards.ContainsKey(n)).ToList();
        if (notFound.Count > 0)
            return BadRequest(new { errors = notFound.Select(n => $"Card not found: {n}") });

        foreach (var (name, qty, isCommander, isSideboard) in parsed)
        {
            if (!cards.TryGetValue(name, out var card)) continue;

            var existing = deck.Cards.FirstOrDefault(c =>
                c.CardId == card.Id &&
                c.IsCommander == isCommander &&
                c.IsSideboard == isSideboard);

            if (existing != null)
                existing.Quantity += qty;
            else
                db.DeckCards.Add(new DeckCard
                {
                    Id = Guid.NewGuid(),
                    DeckId = deck.Id,
                    CardId = card.Id,
                    PrintingId = null, // use default printing; user can update later
                    Quantity = qty,
                    IsCommander = isCommander,
                    IsSideboard = isSideboard,
                });
        }

        deck.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // Reload so navigation properties are populated for the response
        deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstAsync(d => d.Id == id, ct);

        return Ok(ToDetail(deck));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // Parses lines in the format:
    //   4 Lightning Bolt
    //   1 Atraxa, Praetors' Voice *CMDR*
    //   1 Barkchannel Pathway // Tidechannel Pathway
    //   SIDEBOARD:
    //   2 Tormod's Crypt
    private static List<(string Name, int Qty, bool IsCommander, bool IsSideboard)> ParseDecklist(string text)
    {
        var results = new List<(string, int, bool, bool)>();
        var inSideboard = false;

        foreach (var raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            // SIDEBOARD: section marker — all subsequent lines are sideboard cards
            if (line.Equals("SIDEBOARD:", StringComparison.OrdinalIgnoreCase))
            {
                inSideboard = true;
                continue;
            }

            var isCommander = line.Contains("*CMDR*", StringComparison.OrdinalIgnoreCase);
            line = line.Replace("*CMDR*", "", StringComparison.OrdinalIgnoreCase).Trim();

            // Match leading quantity
            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 1) continue;

            if (!int.TryParse(line[..spaceIndex], out var qty)) continue;
            var name = line[(spaceIndex + 1)..].Trim();

            results.Add((name, qty, isCommander, inSideboard));
        }

        return results;
    }

    private static DeckSummaryResponse ToSummary(Deck d) => new(
        d.Id,
        d.Name,
        d.Description,
        d.Format,
        d.Visibility,
        d.Owner.UserName!,
        d.Cards.Sum(c => c.Quantity),
        d.CreatedAt,
        d.UpdatedAt);

    private DeckDetailResponse ToDetail(Deck d)
    {
        var commanderColorIdentity = DeckValidationService.GetCommanderColorIdentity(d.Cards);
        var deckErrors = validator.GetStructuralErrors(d.Cards, d.Format);

        return new DeckDetailResponse(
            d.Id,
            d.Name,
            d.Description,
            d.Primer,
            d.Format,
            d.Visibility,
            d.Owner?.UserName ?? "",
            d.Cards.Select(dc =>
            {
                var printing = dc.Printing ?? dc.Card?.Printings.FirstOrDefault();
                return new DeckCardResponse(
                    dc.Id,
                    dc.CardId,
                    dc.PrintingId,
                    dc.Card?.Name ?? "",
                    dc.Card?.ManaCost,
                    dc.Card?.Cmc ?? 0,
                    dc.Card?.TypeLine ?? "",
                    dc.Card?.ColorIdentity ?? [],
                    printing?.ImageUriNormal,
                    printing?.ImageUriSmall,
                    dc.Quantity,
                    dc.IsCommander,
                    dc.IsSideboard,
                    dc.Card != null
                        ? validator.GetCardErrors(dc, d.Format, commanderColorIdentity)
                        : []);
            }).ToList(),
            deckErrors,
            d.CreatedAt,
            d.UpdatedAt);
    }
}