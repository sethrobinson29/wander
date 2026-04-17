namespace Wander.Client.Helpers;

public static class ColorIdentityHelper
{
    private static readonly Dictionary<string, (int R, int G, int B)> ColorRgb = new()
    {
        ["W"] = (200, 185, 145),
        ["U"] = (14,  55,  130),
        ["B"] = (15,  10,  22),
        ["R"] = (140, 35,  20),
        ["G"] = (20,  75,  35),
    };

    // Returns a full inline style string for a banner div.
    // Edge stops use higher opacity; interior stops dip lower to let the art show through.
    public static string BannerStyle(IEnumerable<string> colors, string? imageUri = null)
    {
        var list = colors.OrderBy(c => "WUBRG".IndexOf(c, StringComparison.Ordinal)).ToList();

        // With image: dramatic dip so art shows in the center.
        // Without image: subtle gradient variation for visual interest.
        var high = imageUri != null ? "0.82" : "0.90";
        var low  = imageUri != null ? "0.52" : "0.78";

        var gradient = BuildGradient(list, high, low);

        return imageUri != null
            ? $"background-image:{gradient},url('{imageUri}');background-size:cover,cover;background-position:center,center;"
            : $"background-image:{gradient};";
    }

    private static string BuildGradient(List<string> colors, string high, string low)
    {
        var rgbs = colors
            .Select(c => ColorRgb.TryGetValue(c, out var v) ? v : (55, 50, 65))
            .ToList();

        if (rgbs.Count == 0)
            rgbs.Add((55, 50, 65)); // colorless

        // Single color: dip in the middle to reveal art in the center.
        if (rgbs.Count == 1)
        {
            var (r, g, b) = rgbs[0];
            return $"linear-gradient(135deg," +
                   $"rgba({r},{g},{b},{high}) 0%," +
                   $"rgba({r},{g},{b},{low}) 50%," +
                   $"rgba({r},{g},{b},{high}) 100%)";
        }

        // Multiple colors: distribute evenly; edges at high opacity, interior stops at low.
        var stops = rgbs.Select((c, i) =>
        {
            var pct = (int)Math.Round(100.0 * i / (rgbs.Count - 1));
            var opacity = (i == 0 || i == rgbs.Count - 1) ? high : low;
            return $"rgba({c.Item1},{c.Item2},{c.Item3},{opacity}) {pct}%";
        });

        return $"linear-gradient(135deg,{string.Join(",", stops)})";
    }
}
