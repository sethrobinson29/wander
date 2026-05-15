using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Wander.Client.Shared;
using Wander.Client.Tests.Helpers;

namespace Wander.Client.Tests.Components;

public class RedirectToLoginTests : BunitTestBase
{
    [Fact]
    public void RedirectToLogin_NavigatesToLoginOnRender()
    {
        RenderComponent<RedirectToLogin>();

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        Assert.EndsWith("/login", nav.Uri);
    }
}
