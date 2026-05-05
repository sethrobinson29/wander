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

    public static string BannerStyle(
    IEnumerable<string> colors,
    string? imageUri = null,
    double? cropLeft = null,
    double? cropTop = null,
    double? cropWidth = null,
    double? cropHeight = null)
    {
        var list = colors.OrderBy(c => "WUBRG".IndexOf(c, StringComparison.Ordinal)).ToList();

        var high = imageUri != null ? "0.82" : "0.90";
        var low = imageUri != null ? "0.52" : "0.78";

        var gradient = BuildGradient(list, high, low);

        if (imageUri == null)
            return $"background-image:{gradient};";

        if (cropLeft.HasValue && cropTop.HasValue && cropWidth.HasValue && cropHeight.HasValue)
        {
            var w = Math.Clamp(cropWidth.Value, 0.01, 0.99);
            var h = Math.Clamp(cropHeight.Value, 0.01, 0.99);
            var l = Math.Clamp(cropLeft.Value, 0, 1 - w);
            var t = Math.Clamp(cropTop.Value, 0, 1 - h);

            var size = (100.0 / w).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var posX = (l / (1.0 - w) * 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            var posY = (t / (1.0 - h) * 100.0).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

            return $"background-image:{gradient},url('{imageUri}');background-size:cover,{size}% auto;background-position:center,{posX}% {posY}%;";
        }

        return $"background-image:{gradient},url('{imageUri}');background-size:cover,cover;background-position:center,center;";
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
