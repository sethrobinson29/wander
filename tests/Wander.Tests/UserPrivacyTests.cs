using Wander.Api.Domain;
using Wander.Api.Services;

namespace Wander.Tests;

public class UserPrivacyTests
{
    // ── Public ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Public_AlwaysVisible(bool isFollower)
    {
        Assert.True(PrivacyService.IsVisible(Privacy.Public, isFollower));
    }

    // ── Restricted ───────────────────────────────────────────────────────────

    [Fact]
    public void Restricted_HiddenFromNonFollower()
    {
        Assert.False(PrivacyService.IsVisible(Privacy.Restricted, isFollower: false));
    }

    [Fact]
    public void Restricted_VisibleToFollower()
    {
        Assert.True(PrivacyService.IsVisible(Privacy.Restricted, isFollower: true));
    }

    // ── Private ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Private_NeverVisible(bool isFollower)
    {
        Assert.False(PrivacyService.IsVisible(Privacy.Private, isFollower));
    }
}