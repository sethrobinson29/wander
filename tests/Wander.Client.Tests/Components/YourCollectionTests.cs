using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Wander.Client.Pages;
using Wander.Client.Tests.Helpers;

namespace Wander.Client.Tests.Components;

public class YourCollectionTests : BunitTestBase
{
    [Fact]
    public void YourCollection_AdminUser_RedirectsToAdmin()
    {
        Auth.SetAuthorized("admin");
        Auth.SetRoles("Admin");
        MockHttp.When("*/decks/mine").Respond("application/json", "[]");

        var cut = RenderComponent<YourCollection>();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        cut.WaitForAssertion(() => Assert.EndsWith("/admin", nav.Uri));
    }

    [Fact]
    public void YourCollection_NonAdmin_EmptyDecks_ShowsEmptyMessage()
    {
        Auth.SetAuthorized("user");
        MockHttp.When("*/decks/mine").Respond("application/json", "[]");

        var cut = RenderComponent<YourCollection>();

        cut.WaitForAssertion(() =>
            Assert.Contains("grimoire is empty", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void YourCollection_NonAdmin_WithDecks_RendersDeckCards()
    {
        Auth.SetAuthorized("user");

        var deckJson = """
            [{
                "id": "11111111-1111-1111-1111-111111111111",
                "name": "Test Deck",
                "description": null,
                "format": 0,
                "coverImageUri": null,
                "coverCropLeft": null,
                "coverCropTop": null,
                "coverCropWidth": null,
                "coverCropHeight": null,
                "colorIdentity": [],
                "visibility": 0,
                "ownerUsername": "user",
                "cardCount": 60,
                "createdAt": "2025-01-01T00:00:00Z",
                "updatedAt": "2025-01-01T00:00:00Z"
            }]
            """;

        MockHttp.When("*/decks/mine").Respond("application/json", deckJson);

        var cut = RenderComponent<YourCollection>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Test Deck", cut.Markup));
    }
}
