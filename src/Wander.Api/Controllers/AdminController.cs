using Microsoft.AspNetCore.Mvc;
using Wander.Api.Infrastructure.Scryfall;

namespace Wander.Api.Controllers;

[ApiController]
[Route("admin")]
public class AdminController(ScryfallBulkDataService syncService) : ControllerBase
{
    [HttpPost("sync")]
    public async Task<IActionResult> TriggerSync(CancellationToken cancellationToken)
    {
        await syncService.SyncAsync(cancellationToken);
        return Ok(new { message = "Sync complete." });
    }
}