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
[Route("")]
public class CommentController(WanderDbContext db, ActivityService activity, NotificationService notifications) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("decks/{deckId}/comments")]
    public async Task<ActionResult<List<CommentResponse>>> GetComments(Guid deckId)
    {
        var deck = await db.Decks.FindAsync(deckId);
        if (deck is null) return NotFound();

        // Only visible decks (public, or owner, or unlisted with direct link)
        if (deck.Visibility == Visibility.Private && deck.OwnerId != UserId)
            return NotFound();

        var comments = await db.DeckComments
            .Where(c => c.DeckId == deckId && c.ParentCommentId == null && !c.IsDeleted)
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments.Select(MapComment).ToList());
    }

    [HttpPost("decks/{deckId}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentResponse>> PostComment(Guid deckId,
        [FromBody] PostCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 2000)
            return BadRequest(new { error = "Comment must be between 1 and 2000 characters." });

        var deck = await db.Decks.FindAsync(deckId);
        if (deck is null) return NotFound();
        if (deck.Visibility == Visibility.Private && deck.OwnerId != UserId)
            return NotFound();

        var comment = new DeckComment
        {
            DeckId = deckId,
            AuthorId = UserId!,
            Body = request.Body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.DeckComments.Add(comment);
        activity.Record(UserId!, ActivityType.CommentedOnDeck, targetId: deckId.ToString(), targetName: deck.Name);
        await db.SaveChangesAsync();

        await notifications.NotifyAsync(
            recipientId: deck.OwnerId,
            actorId: UserId!,
            type: NotificationType.DeckCommented,
            deckId: deck.Id,
            deckName: deck.Name,
            actorUsername: User.Identity!.Name);

        await db.Entry(comment).Reference(c => c.Author).LoadAsync();
        return CreatedAtAction(nameof(GetComments), new { deckId }, MapComment(comment));
    }

    [HttpPost("comments/{commentId}/replies")]
    [Authorize]
    public async Task<ActionResult<CommentResponse>> PostReply(Guid commentId,
        [FromBody] PostCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 2000)
            return BadRequest(new { error = "Reply must be between 1 and 2000 characters." });

        var parent = await db.DeckComments
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == commentId);
        if (parent is null) return NotFound();

        // Only allow replying to top-level comments — not to replies
        if (!CommentGuard.IsTopLevel(parent))
            return BadRequest(new { error = "Cannot reply to a reply." });

        if (parent.Deck.Visibility == Visibility.Private && parent.Deck.OwnerId != UserId)
            return NotFound();

        var reply = new DeckComment
        {
            DeckId = parent.DeckId,
            AuthorId = UserId!,
            ParentCommentId = commentId,
            Body = request.Body.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.DeckComments.Add(reply);
        await db.SaveChangesAsync();

        await db.Entry(reply).Reference(r => r.Author).LoadAsync();
        return Created("", MapCommentFlat(reply));
    }

    [HttpPut("comments/{id}")]
    [Authorize]
    public async Task<IActionResult> EditComment(Guid id, [FromBody] PostCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 2000)
            return BadRequest(new { error = "Comment must be between 1 and 2000 characters." });

        var comment = await db.DeckComments.FindAsync(id);
        if (comment is null || comment.IsDeleted) return NotFound();
        if (comment.AuthorId != UserId) return Forbid();

        comment.Body = request.Body.Trim();
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("comments/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var comment = await db.DeckComments.FindAsync(id);
        if (comment is null) return NotFound();

        // Allow author or deck owner to delete
        var deck = await db.Decks.FindAsync(comment.DeckId);
        if (comment.AuthorId != UserId && deck?.OwnerId != UserId) return Forbid();

        comment.IsDeleted = true;
        comment.Body = "[deleted]";
        comment.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CommentResponse MapComment(DeckComment c) => new(
        c.Id, c.AuthorId, c.Author.UserName!, c.Author.AvatarId,
        c.IsDeleted ? "[deleted]" : c.Body,
        c.IsDeleted, c.CreatedAt, c.UpdatedAt,
        c.Replies
            .Where(r => !r.IsDeleted)
            .OrderBy(r => r.CreatedAt)
            .Select(MapCommentFlat)
            .ToList());

    private static CommentResponse MapCommentFlat(DeckComment c) => new(
        c.Id, c.AuthorId, c.Author.UserName!, c.Author.AvatarId,
        c.IsDeleted ? "[deleted]" : c.Body,
        c.IsDeleted, c.CreatedAt, c.UpdatedAt,
        []);
}