using System.Text.RegularExpressions;

namespace Wander.Api.Services;

public static class MarkdownValidator
{
    // Detects HTML tags, javascript:/data: protocols, and inline event handlers.
    private static readonly Regex DangerousPattern =
        new(@"<[^>]+>|javascript\s*:|data\s*:\s*text/html|on\w+\s*=",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns null if valid, or an error message if the primer contains unsafe content.
    /// </summary>
    public static string? ValidatePrimer(string? primer)
    {
        if (string.IsNullOrWhiteSpace(primer))
            return null;

        if (DangerousPattern.IsMatch(primer))
            return "Primer contains invalid content. HTML tags and scripts are not permitted.";

        return null;
    }
}
