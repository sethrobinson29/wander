using Wander.E2E.Helpers;

namespace Wander.E2E.Smoke;

[Trait("Category", "E2E")]
public class BrowseSmokeTests : E2ETestBase
{
    [Fact]
    public async Task Browse_PageLoads_ShowsBrowseTitle()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await WaitForBlazorAsync();

        var content = await Page.ContentAsync();
        Assert.Contains("Browse Decks", content);
    }

    [Fact]
    public async Task Browse_SearchByName_NavigatesToSearch()
    {
        await Page.GotoAsync($"{BaseUrl}/");
        await WaitForBlazorAsync();

        // SearchBar MudTextField renders with placeholder="Search..."
        await Page.Locator("input[placeholder='Search...']").FillAsync("lightning");
        await Page.Keyboard.PressAsync("Enter");

        await Page.WaitForURLAsync(url => url.Contains("/search"), new() { Timeout = 10_000 });
        Assert.Contains("/search", Page.Url);
    }
}
