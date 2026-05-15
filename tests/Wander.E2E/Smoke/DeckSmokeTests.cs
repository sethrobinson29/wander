using Wander.E2E.Helpers;

namespace Wander.E2E.Smoke;

[Trait("Category", "E2E")]
public class DeckSmokeTests : E2ETestBase
{
    // LoginAsync already navigates to /decks on success — just wait for the component.
    private async Task GoToDecksAsync()
    {
        await LoginAsync();
        await Page.WaitForSelectorAsync("button:has-text('New Deck')", new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task Decks_NewDeckButton_OpensCreateDialog()
    {
        await GoToDecksAsync();
        await Page.Locator("button:has-text('New Deck')").ClickAsync();

        await Page.WaitForSelectorAsync(".mud-dialog, [role='dialog']", new() { Timeout = 5_000 });
        Assert.True(await Page.Locator(".mud-dialog").CountAsync() > 0);
    }

    [Fact]
    public async Task Decks_CreateDeck_AppearsInCollection()
    {
        await GoToDecksAsync();

        await Page.Locator("button:has-text('New Deck')").ClickAsync();
        await Page.WaitForSelectorAsync(".mud-dialog", new() { Timeout = 5_000 });

        var deckName = $"Smoke Test {Guid.NewGuid().ToString("N")[..8]}";
        await Page.Locator(".mud-dialog input[type='text']").First.FillAsync(deckName);
        await Page.Locator(".mud-dialog button:has-text('Create')").ClickAsync();

        await Page.WaitForURLAsync(url => url.Contains("/decks/"), new() { Timeout = 10_000 });
        Assert.Contains("/decks/", Page.Url);
    }
}
