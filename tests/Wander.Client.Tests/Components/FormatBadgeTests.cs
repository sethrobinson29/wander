using Wander.Client.Models;
using Wander.Client.Shared;
using Wander.Client.Tests.Helpers;

namespace Wander.Client.Tests.Components;

public class FormatBadgeTests : BunitTestBase
{
    [Theory]
    [InlineData(Format.Commander, "Commander")]
    [InlineData(Format.Standard,  "Standard")]
    [InlineData(Format.Modern,    "Modern")]
    [InlineData(Format.Legacy,    "Legacy")]
    [InlineData(Format.Vintage,   "Vintage")]
    [InlineData(Format.Pauper,    "Pauper")]
    public void FormatBadge_RendersFormatName(Format format, string expectedText)
    {
        var cut = RenderComponent<FormatBadge>(p => p.Add(c => c.Format, format));

        Assert.Contains(expectedText, cut.Markup);
    }
}
