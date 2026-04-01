using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Wander.Api.Infrastructure.Data;
using Wander.Api.Models.Cards;

namespace Wander.Api.Controllers;

[ApiController]
[Route("cards")]
public class CardController(WanderDbContext db) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<List<CardSearchResponse>>> Search(
        [FromQuery] string q,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(new { error = "Query must be at least 2 characters." });

        var tsQuery = string.Join(" & ", q.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w + ":*"));

        var results = await db.Cards
            .Where(c => c.NameSearchVector!.Matches(EF.Functions.ToTsQuery("english", tsQuery)))
            .OrderBy(c => c.Name)
            .Take(20)
            .ToListAsync(ct);

        return Ok(results.Select(c => new CardSearchResponse(
            c.Id,
            c.Name,
            c.ManaCost,
            c.Cmc,
            c.TypeLine,
            c.OracleText,
            c.Colors,
            c.ColorIdentity,
            c.ImageUriNormal,
            c.ImageUriSmall,
            c.Legalities)));
    }
}