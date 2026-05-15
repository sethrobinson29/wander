using Wander.E2E.Helpers;

namespace Wander.E2E.Smoke;

[Trait("Category", "E2E")]
public class AuthSmokeTests : E2ETestBase
{
    [Fact]
    public async Task Login_ValidCredentials_RedirectsAwayFromLogin()
    {
        await LoginAsync();

        Assert.DoesNotContain("/login", Page.Url);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        await Page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 30_000 });

        await Page.Locator("input[type='email']").FillAsync("bad@example.com");
        await Page.Locator("input[type='password']").FillAsync("wrongpassword");
        await Page.Locator("button:has-text('Enter the Archive')").ClickAsync();

        await Page.WaitForTimeoutAsync(2000);
        Assert.Contains("/login", Page.Url);
    }

    [Fact]
    public async Task Logout_ClearsSessionAndRedirects()
    {
        await LoginAsync();
        await Page.Locator("button:has-text('Log Out')").ClickAsync();

        await Page.WaitForTimeoutAsync(1000);
        Assert.Contains("Log In", await Page.ContentAsync());
    }
}
