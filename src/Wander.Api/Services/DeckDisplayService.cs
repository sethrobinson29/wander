using Wander.Api.Domain;

namespace Wander.Api.Services;

public static class DeckDisplayService
{
    public static string? ResolveCoverImage(Deck d)
    {
        if (d.CoverPrinting != null) return d.CoverPrinting.ImageUriArtCrop;
        var commander = d.Cards.FirstOrDefault(c => c.IsCommander);
        var printing = commander?.Printing ?? commander?.Card?.Printings.FirstOrDefault();
        return printing?.ImageUriArtCrop;
    }
}
