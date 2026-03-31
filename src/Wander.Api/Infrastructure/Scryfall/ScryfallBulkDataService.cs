using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Wander.Api.Domain;
using Wander.Api.Infrastructure.Data;

namespace Wander.Api.Infrastructure.Scryfall;

public class ScryfallBulkDataService(
    HttpClient httpClient,
    WanderDbContext db,
    ILogger<ScryfallBulkDataService> logger)
{
    private const string BulkDataUrl = "https://api.scryfall.com/bulk-data";

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Scryfall sync...");

        var downloadUri = await GetBulkDownloadUriAsync(cancellationToken);
        if (downloadUri is null)
        {
            logger.LogError("Could not find oracle_cards bulk data entry.");
            return;
        }

        var existing = db.Cards
            .Select(c => new { c.ScryfallId, c.Id })
            .ToDictionary(c => c.ScryfallId, c => c.Id);

        var count = 0;
        await foreach (var card in StreamCardsAsync(downloadUri, cancellationToken))
        {
            UpsertCard(card, existing);
            count++;

            if (count % 1000 == 0)
            {
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Upserted {Count} cards...", count);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Scryfall sync complete. Total cards: {Count}", count);
    }

    private async Task<string?> GetBulkDownloadUriAsync(CancellationToken ct)
    {
        var response = await httpClient.GetFromJsonAsync<BulkDataResponse>(BulkDataUrl, ct);
        return response?.Data
            .FirstOrDefault(e => e.Type == "oracle_cards")
            ?.DownloadUri;
    }

    private async IAsyncEnumerable<ScryfallCard> StreamCardsAsync(
        string uri,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await httpClient.GetStreamAsync(uri, ct);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        await foreach (var card in JsonSerializer.DeserializeAsyncEnumerable<ScryfallCard>(
            stream, options, ct))
        {
            if (card is not null && card.ImageUris is not null)
                yield return card;
        }
    }

    private void UpsertCard(ScryfallCard src, Dictionary<string, Guid> existing)
    {
        if (!existing.TryGetValue(src.Id, out var existingId))
        {
            db.Cards.Add(new Card
            {
                Id = Guid.NewGuid(),
                ScryfallId = src.Id,
                Name = src.Name,
                ManaCost = src.ManaCost,
                Cmc = src.Cmc,
                TypeLine = src.TypeLine,
                OracleText = src.OracleText,
                Colors = src.Colors ?? [],
                ColorIdentity = src.ColorIdentity,
                ImageUriSmall = src.ImageUris?.Small,
                ImageUriNormal = src.ImageUris?.Normal,
                ImageUriArtCrop = src.ImageUris?.ArtCrop,
                SetCode = src.Set,
                CollectorNumber = src.CollectorNumber,
                Legalities = src.Legalities,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            var tracked = db.Cards.Find(existingId)!;
            tracked.Name = src.Name;
            tracked.ManaCost = src.ManaCost;
            tracked.Cmc = src.Cmc;
            tracked.TypeLine = src.TypeLine;
            tracked.OracleText = src.OracleText;
            tracked.Colors = src.Colors ?? [];
            tracked.ColorIdentity = src.ColorIdentity;
            tracked.ImageUriSmall = src.ImageUris?.Small;
            tracked.ImageUriNormal = src.ImageUris?.Normal;
            tracked.ImageUriArtCrop = src.ImageUris?.ArtCrop;
            tracked.SetCode = src.Set;
            tracked.CollectorNumber = src.CollectorNumber;
            tracked.Legalities = src.Legalities;
            tracked.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}