using Wander.Api.Services;

namespace Wander.Tests;

public class AvatarTests
{
    [Theory]
    [InlineData(null, true)]   // null = default, always valid
    [InlineData("lotus", true)]
    [InlineData("bolt", true)]
    [InlineData("scholar", true)]
    [InlineData("photo", false)]  // upload path — not a preset
    [InlineData("", false)]
    [InlineData("LOTUS", false)]  // case-sensitive
    public void IsValidAvatarId(string? id, bool expected) =>
        Assert.Equal(expected, AvatarService.IsValidAvatarId(id));

    [Fact]
    public void GetAvatarHue_IsDeterministic()
    {
        var hue1 = AvatarService.GetAvatarHue("sethp");
        var hue2 = AvatarService.GetAvatarHue("sethp");
        Assert.Equal(hue1, hue2);
    }

    [Fact]
    public void GetAvatarHue_IsInRange()
    {
        var hue = AvatarService.GetAvatarHue("anyone");
        Assert.InRange(hue, 0, 359);
    }

    [Fact]
    public void GetAvatarHue_DifferentUsernames_CanDiffer()
    {
        var hues = new[] { "alice", "bob", "charlie", "sethp", "wanderer" }
            .Select(AvatarService.GetAvatarHue)
            .ToHashSet();
        Assert.True(hues.Count > 1, "Expected multiple different hues across distinct usernames.");
    }
}
