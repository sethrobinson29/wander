using System.Text.RegularExpressions;
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
        if (q.Length > 200)
            return BadRequest(new { error = "Query must be at most 200 characters." });

        // Strip tsquery operator characters so user input cannot inject query syntax
        var terms = q.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => Regex.Replace(w, @"[^\p{L}\p{N}'\-]", ""))
            .Where(w => w.Length > 0)
            .ToList();

        if (terms.Count == 0)
            return BadRequest(new { error = "Query must contain at least one word character." });

        var tsQuery = string.Join(" & ", terms.Select(w => w + ":*"));

        var results = await db.Cards
            .Where(c => c.NameSearchVector!.Matches(EF.Functions.ToTsQuery("english", tsQuery)))
            .Include(c => c.Printings)
            .OrderBy(c => c.Name)
            .Take(20)
            .ToListAsync(ct);

        return Ok(results.Select(c =>
        {
            var printing = c.Printings.FirstOrDefault();
            return new CardSearchResponse(
                c.Id,
                c.Name,
                c.ManaCost,
                c.Cmc,
                c.TypeLine,
                c.OracleText,
                c.Colors,
                c.ColorIdentity,
                printing?.Id,
                printing?.ImageUriNormal,
                printing?.ImageUriSmall,
                c.Legalities);
        }));
    }
}
