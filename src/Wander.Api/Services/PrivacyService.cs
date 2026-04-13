using Wander.Api.Domain;

namespace Wander.Api.Services;

internal static class PrivacyService
{
    /// <summary>
    /// Returns true if a field with the given privacy setting should be shown to the viewer.
    /// </summary>
    /// <param name="privacy">The field's privacy setting.</param>
    /// <param name="isFollower">Whether the viewer follows the profile owner.</param>
    public static bool IsVisible(Privacy privacy, bool isFollower) => privacy switch
    {
        Privacy.Public => true,
        Privacy.Restricted => isFollower,
        Privacy.Private => false,
        _ => false,
    };
}