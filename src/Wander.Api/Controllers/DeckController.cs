using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Decks;
using Wander.Api.Services;
using static Wander.Api.Services.DeckDisplayService;

namespace Wander.Api.Controllers;

[ApiController]
[Route("decks")]
public class DeckController(WanderDbContext db, DeckValidationService validator, ActivityService activity, NotificationService notifications) : ControllerBase
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
            .Include(d => d.Cards).ThenInclude(dc => dc.Card);

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
            .Include(d => d.Cards).ThenInclude(dc => dc.Card)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync(ct);


        return Ok(decks.Select(ToSummary));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeckDetailResponse>> Get(Guid id, CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.CoverPrinting)
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

        var (likeCount, isLiked) = await GetLikeInfoAsync(id, ct);
        return Ok(ToDetail(deck, likeCount, isLiked));
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<DeckSummaryResponse>>> Search(
    [FromQuery] string q,
    [FromQuery] string type = "name",
    [FromQuery] Format? format = null,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<DeckSummaryResponse>());

        IQueryable<Deck> query = db.Decks
            .Where(d => d.Visibility == Visibility.Public)
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card)
            .Include(d => d.CoverPrinting);

        var lowerQ = q.ToLower();
        query = type switch
        {
            "commander" => query.Where(d => d.Cards.Any(c => c.IsCommander && c.Card!.Name.ToLower().Contains(lowerQ))),
            _ => query.Where(d => d.Name.ToLower().Contains(lowerQ))
        };

        if (format.HasValue) query = query.Where(d => d.Format == format.Value);

        var decks = await query.OrderByDescending(d => d.UpdatedAt).Take(50).ToListAsync(ct);
        return Ok(decks.Select(d => ToSummary(d)));
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

        return CreatedAtAction(nameof(Get), new { id = deck.Id }, ToDetail(deck, 0, false));
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
            .Include(d => d.CoverPrinting)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        if (!Enum.IsDefined(request.Format)) return BadRequest(new { error = "Invalid format." });
        if (!Enum.IsDefined(request.Visibility)) return BadRequest(new { error = "Invalid visibility." });

        var primerError = MarkdownValidator.ValidatePrimer(request.Primer);
        if (primerError != null) return BadRequest(new { error = primerError });

        var previous = deck.Visibility;

        deck.Name = request.Name;
        deck.Description = request.Description;
        deck.Primer = request.Primer;
        deck.Format = request.Format;
        deck.Visibility = request.Visibility;
        deck.UpdatedAt = DateTimeOffset.UtcNow;

        if (ActivityService.IsMadePublic(previous, request.Visibility))
            activity.Record(deck.OwnerId, ActivityType.MadeDeckPublic, targetId: deck.Id.ToString(), targetName: deck.Name);
        await db.SaveChangesAsync(ct);

        if (ActivityService.IsMadePublic(previous, request.Visibility))
            await notifications.NotifyFollowersAsync(deck.OwnerId, User.Identity!.Name!, deck.Id, deck.Name);

        var (likeCount, isLiked) = await GetLikeInfoAsync(id, ct);
        return Ok(ToDetail(deck, likeCount, isLiked));
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

    [HttpPost("{id}/like")]
    [Authorize]
    public async Task<IActionResult> Like(Guid id)
    {
        // Only public or owned decks can be liked
        var deck = await db.Decks.FindAsync(id);
        if (deck is null) return NotFound();
        if (deck.Visibility != Visibility.Public && deck.OwnerId != UserId) return NotFound();

        var exists = await db.DeckLikes.AnyAsync(l => l.DeckId == id && l.UserId == UserId);
        if (exists) return Conflict();

        db.DeckLikes.Add(new DeckLike
        {
            UserId = UserId,
            DeckId = id,
            CreatedAt = DateTimeOffset.UtcNow
        });;
        activity.Record(UserId, ActivityType.LikedDeck, targetId: id.ToString(), targetName: deck.Name);
        await db.SaveChangesAsync();
        await notifications.NotifyAsync(
            recipientId: deck.OwnerId,
            actorId: UserId,
            type: NotificationType.DeckLiked,
            deckId: deck.Id,
            deckName: deck.Name,
            actorUsername: User.Identity!.Name);

        return NoContent();
    }

    [HttpDelete("{id}/like")]
    [Authorize]
    public async Task<IActionResult> Unlike(Guid id)
    {
        var like = await db.DeckLikes
            .FirstOrDefaultAsync(l => l.DeckId == id && l.UserId == UserId);
        if (like is null) return NotFound();

        db.DeckLikes.Remove(like);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/cover")]
    [Authorize]
    public async Task<IActionResult> SetCover(Guid id, [FromBody] SetCoverRequest request, CancellationToken ct)
    {
        var deck = await db.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        if (request.PrintingId.HasValue)
        {
            // Validate that the printing belongs to a card currently in the deck
            var deckCardIds = deck.Cards.Select(c => c.CardId).ToList();
            var printing = await db.CardPrintings
                .FirstOrDefaultAsync(p => p.Id == request.PrintingId.Value && deckCardIds.Contains(p.CardId), ct);
            if (printing is null) return BadRequest(new { error = "Printing not found in this deck." });
        }

        deck.CoverPrintingId = request.PrintingId;
        deck.CoverCropLeft = request.CropLeft;
        deck.CoverCropTop = request.CropTop;
        deck.CoverCropWidth = request.CropWidth;
        deck.CoverCropHeight = request.CropHeight;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("cards/{deckCardId:guid}/printing"), Authorize]
    public async Task<IActionResult> UpdateCardPrinting(
    Guid deckCardId,
    [FromBody] UpdateCardPrintingRequest request,
    CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var deckCard = await db.DeckCards
            .Include(dc => dc.Deck)
            .Include(dc => dc.Card).ThenInclude(c => c!.Printings)
            .FirstOrDefaultAsync(dc => dc.Id == deckCardId, ct);

        if (deckCard == null) return NotFound();
        if (deckCard.Deck.OwnerId != userId) return Forbid();

        if (request.PrintingId.HasValue)
        {
            var valid = deckCard.Card?.Printings.Any(p => p.Id == request.PrintingId.Value) ?? false;
            if (!valid) return BadRequest("Printing does not belong to this card.");
            deckCard.PrintingId = request.PrintingId.Value;
        }
        else
        {
            deckCard.PrintingId = null;
        }

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
            .Include(d => d.CoverPrinting)
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
        var (likeCount, isLiked) = await GetLikeInfoAsync(id, ct);
        return Ok(ToDetail(deck, likeCount, isLiked));
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
            .Include(d => d.CoverPrinting)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (deck is null) return NotFound();
        if (deck.OwnerId != UserId) return Forbid();

        var parsed = DecklistParser.Parse(request.Decklist);

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

        bool commandersCleared = false;
        foreach (var (name, qty, isCommander, isSideboard) in parsed)
        {
            if (!cards.TryGetValue(name, out var card)) continue;

            if (isCommander)
            {
                // Clear existing commanders once so re-importing replaces the command zone cleanly.
                if (!commandersCleared)
                {
                    var existingCommanders = deck.Cards.Where(c => c.IsCommander).ToList();
                    db.DeckCards.RemoveRange(existingCommanders);
                    foreach (var c in existingCommanders) deck.Cards.Remove(c);
                    commandersCleared = true;
                }

                if (deck.Cards.Count(c => c.IsCommander) >= 2)
                    return BadRequest(new { errors = new[] { "A deck cannot have more than two commanders." } });

                if (deck.Cards.Any(c => c.CardId == card.Id && c.IsCommander)) continue;

                db.DeckCards.Add(new DeckCard
                {
                    Id = Guid.NewGuid(),
                    DeckId = deck.Id,
                    CardId = card.Id,
                    PrintingId = null,
                    Quantity = 1,
                    IsCommander = true,
                    IsSideboard = false,
                });
            }
            else
            {
                var existing = deck.Cards.FirstOrDefault(c =>
                    c.CardId == card.Id &&
                    !c.IsCommander &&
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
                        IsCommander = false,
                        IsSideboard = isSideboard,
                    });
            }
        }

        deck.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        // Reload so navigation properties are populated for the response
        deck = await db.Decks
            .Include(d => d.Owner)
            .Include(d => d.Cards).ThenInclude(dc => dc.Card).ThenInclude(c => c.Printings)
            .Include(d => d.Cards).ThenInclude(dc => dc.Printing)
            .FirstAsync(d => d.Id == id, ct);

        var (likeCount, isLiked) = await GetLikeInfoAsync(id, ct);
        return Ok(ToDetail(deck, likeCount, isLiked));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DeckSummaryResponse ToSummary(Deck d) => new(
    d.Id,
    d.Name,
    d.Description,
    d.Format,
    ResolveCoverImage(d),
    d.CoverCropLeft,
    d.CoverCropTop,
    d.CoverCropWidth,
    d.CoverCropHeight,
    (d.Cards.Any(c => c.IsCommander)
        ? d.Cards.Where(c => c.IsCommander).SelectMany(c => c.Card?.ColorIdentity ?? [])
        : d.Cards.SelectMany(c => c.Card?.ColorIdentity ?? []))
        .Distinct()
        .OrderBy(c => "WUBRG".IndexOf(c, StringComparison.Ordinal))
        .ToList(),
    d.Visibility,
    d.Owner.UserName!,
    d.Cards.Where(c => !c.IsSideboard).Sum(c => c.Quantity),
    d.CreatedAt,
    d.UpdatedAt);

    private async Task<(int LikeCount, bool IsLiked)> GetLikeInfoAsync(Guid deckId, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var likeCount = await db.DeckLikes.CountAsync(l => l.DeckId == deckId, ct);
        var isLiked = userId != null &&
                        await db.DeckLikes.AnyAsync(l => l.DeckId == deckId && l.UserId == userId, ct);
        return (likeCount, isLiked);
    }

    private DeckDetailResponse ToDetail(Deck d, int likeCount, bool isLiked)
    {
        var commanderColorIdentity = DeckValidationService.GetCommanderColorIdentity(d.Cards);
        var deckErrors = validator.GetStructuralErrors(d.Cards, d.Format);

        return new DeckDetailResponse(
            d.Id,
            d.Name,
            d.Description,
            d.Primer,
            d.Format,
            ResolveCoverImage(d),
            d.CoverCropLeft,
            d.CoverCropTop,
            d.CoverCropWidth,
            d.CoverCropHeight,
            d.Visibility,
            d.OwnerId,
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
                    dc.Card?.OracleText,
                    printing?.FlavorText,
                    dc.Card?.Legalities ?? new Dictionary<string, string>(),
                    dc.Card?.ColorIdentity ?? [],
                    printing?.ImageUriNormal,
                    printing?.ImageUriSmall,
                    printing?.ImageUriArtCrop,
                    printing?.Id,
                    dc.Quantity,
                    dc.IsCommander,
                    dc.IsSideboard,
                    dc.Card != null
                        ? validator.GetCardErrors(dc, d.Format, commanderColorIdentity)
                        : []);
            }).ToList(),
            deckErrors,
            d.CreatedAt,
            d.UpdatedAt,
            likeCount,
            isLiked);
    }

}