using Microsoft.Playwright;

namespace Wander.E2E.Helpers;

public abstract class E2ETestBase : IAsyncLifetime
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    public static string TestEmail =>
        Environment.GetEnvironmentVariable("E2E_TEST_EMAIL") ?? "test@example.com";

    public static string TestPassword =>
        Environment.GetEnvironmentVariable("E2E_TEST_PASSWORD") ?? "Test@1234!";

    // Non-admin user for tests that require user-specific features (deck creation, etc.)
    // Falls back to TestEmail/TestPassword if not set separately.
    public static string UserEmail =>
        Environment.GetEnvironmentVariable("E2E_TEST_USER_EMAIL") ?? TestEmail;

    public static string UserPassword =>
        Environment.GetEnvironmentVariable("E2E_TEST_USER_PASSWORD") ?? TestPassword;

    protected IPage Page { get; private set; } = null!;
    private IBrowser _browser = null!;
    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    // Wait for Blazor WASM to finish bootstrapping by waiting for a MudBlazor
    // element in the app shell — cheaper than NetworkIdle, more reliable than
    // arbitrary delays.
    protected async Task WaitForBlazorAsync() =>
        await Page.WaitForSelectorAsync(".mud-appbar, .mud-main-content",
            new() { Timeout = 30_000 });

    protected async Task LoginAsync()
    {
        await Page.GotoAsync($"{BaseUrl}/login");
        // Wait for the login form — confirms WASM has rendered
        await Page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 30_000 });

        await Page.Locator("input[type='email']").FillAsync(TestEmail);
        await Page.Locator("input[type='password']").FillAsync(TestPassword);
        // Button text in Login.razor is "Enter the Archive"
        await Page.Locator("button:has-text('Enter the Archive')").ClickAsync();
        await Page.WaitForURLAsync(url => !url.Contains("/login"), new() { Timeout = 15_000 });
    }
}
