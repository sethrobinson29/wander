using System.Text.Json.Serialization;

namespace Wander.Api.Infrastructure.Scryfall;

// Response from https://api.scryfall.com/bulk-data
public record BulkDataResponse(
    [property: JsonPropertyName("data")] List<BulkDataEntry> Data
);

public record BulkDataEntry(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("download_uri")] string DownloadUri,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt
);

// One card entry from the bulk JSON
public record ScryfallCard(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("mana_cost")] string? ManaCost,
    [property: JsonPropertyName("cmc")] decimal Cmc,
    [property: JsonPropertyName("type_line")] string TypeLine,
    [property: JsonPropertyName("oracle_text")] string? OracleText,
    [property: JsonPropertyName("colors")] List<string>? Colors,
    [property: JsonPropertyName("color_identity")] List<string> ColorIdentity,
    [property: JsonPropertyName("image_uris")] ScryfallImageUris? ImageUris,
    [property: JsonPropertyName("card_faces")] List<ScryfallCardFace>? CardFaces,
    [property: JsonPropertyName("set")] string Set,
    [property: JsonPropertyName("collector_number")] string CollectorNumber,
    [property: JsonPropertyName("legalities")] Dictionary<string, string> Legalities,
    [property: JsonPropertyName("layout")] string Layout
)
{
    // For double-faced cards, image_uris is null and images live on the first card face.
    public ScryfallImageUris? EffectiveImageUris => ImageUris ?? CardFaces?.FirstOrDefault()?.ImageUris;
}

public record ScryfallCardFace(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("image_uris")] ScryfallImageUris? ImageUris
);

public record ScryfallImageUris(
    [property: JsonPropertyName("small")] string? Small,
    [property: JsonPropertyName("normal")] string? Normal,
    [property: JsonPropertyName("art_crop")] string? ArtCrop
);
