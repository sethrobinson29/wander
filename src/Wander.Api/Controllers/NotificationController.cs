using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Controllers;

[ApiController]
[Route("notifications")]
[Authorize]
public class NotificationController(WanderDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public record NotificationResponse(
        Guid Id,
        string Type,
        string ActorId,
        string? ActorUsername,
        Guid? DeckId,
        string? DeckName,
        bool IsRead,
        DateTimeOffset CreatedAt);

    public record NotificationListResponse(int UnreadCount, List<NotificationResponse> Items);

    // GET /notifications?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<NotificationListResponse>> GetNotifications(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var unreadCount = await db.Notifications
            .CountAsync(n => n.RecipientId == UserId && !n.IsRead);

        var items = await db.Notifications
            .Where(n => n.RecipientId == UserId)
            .Include(n => n.Actor)
            .OrderBy(n => n.IsRead)              // unread first
            .ThenByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationResponse(
                n.Id, n.Type.ToString(),
                n.ActorId, n.Actor.UserName,
                n.DeckId, n.DeckName,
                n.IsRead, n.CreatedAt))
            .ToListAsync();

        return Ok(new NotificationListResponse(unreadCount, items));
    }

    // PUT /notifications/{id}/read
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == UserId);
        if (notification is null) return NotFound();

        notification.IsRead = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // PUT /notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await db.Notifications
            .Where(n => n.RecipientId == UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return NoContent();
    }
}
