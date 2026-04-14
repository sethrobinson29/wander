namespace Wander.Api.Services;

internal static class AvatarService
{
    public static readonly HashSet<string> Allowed = ["lotus", "bolt", "scholar"];

    public static bool IsValidAvatarId(string? id) =>
        id is null || Allowed.Contains(id);

    // Exposed for testing — deterministic hue from username
    public static int GetAvatarHue(string username) =>
        Math.Abs(username.GetHashCode()) % 360;
}