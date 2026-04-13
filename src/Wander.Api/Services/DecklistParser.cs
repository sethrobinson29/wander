namespace Wander.Api.Services;

internal static class DecklistParser
{
    // Parses lines in the format:
    //   4 Lightning Bolt
    //   1 Atraxa, Praetors' Voice *CMDR*
    //   1 Barkchannel Pathway // Tidechannel Pathway
    //   SIDEBOARD:
    //   2 Tormod's Crypt
    public static List<(string Name, int Qty, bool IsCommander, bool IsSideboard)> Parse(string text)
    {
        var results = new List<(string, int, bool, bool)>();
        var inSideboard = false;

        foreach (var raw in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

            if (line.Equals("SIDEBOARD:", StringComparison.OrdinalIgnoreCase))
            {
                inSideboard = true;
                continue;
            }

            var isCommander = line.Contains("*CMDR*", StringComparison.OrdinalIgnoreCase);
            line = line.Replace("*CMDR*", "", StringComparison.OrdinalIgnoreCase).Trim();

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 1) continue;

            if (!int.TryParse(line[..spaceIndex], out var qty)) continue;
            var name = line[(spaceIndex + 1)..].Trim();

            results.Add((name, qty, isCommander, inSideboard));
        }

        return results;
    }
}