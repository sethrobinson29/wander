using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using RichardSzalay.MockHttp;
using Wander.Client.Services;

namespace Wander.Client.Tests.Helpers;

public abstract class BunitTestBase : TestContext
{
    protected readonly MockHttpMessageHandler MockHttp;
    protected readonly TestAuthorizationContext Auth;

    protected BunitTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();

        MockHttp = new MockHttpMessageHandler();
        MockHttp.Fallback.Respond("application/json", "null");

        var httpClient = MockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5156/");

        Services.AddScoped<LocalStorage>();
        Services.AddScoped<WanderApiClient>(sp =>
            new WanderApiClient(httpClient, sp.GetRequiredService<LocalStorage>()));
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        Auth = this.AddTestAuthorization();

        // MudBlazor components that use MudPopoverBase (MudSelect, MudMenu, etc.)
        // require MudPopoverProvider to be rendered so PopoverService.CreatePopoverAsync
        // doesn't throw. Rendering it once here satisfies the service for all tests.
        this.RenderComponent<MudPopoverProvider>();
    }
}
