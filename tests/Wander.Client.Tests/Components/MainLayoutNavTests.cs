using Bunit;
using Wander.Client.Layout;
using Wander.Client.Shared;
using Wander.Client.Tests.Helpers;

namespace Wander.Client.Tests.Components;

public class MainLayoutNavTests : BunitTestBase
{
    public MainLayoutNavTests()
    {
        // Stub child components that have complex deps (SignalR, MudSelect popovers)
        // so MainLayout nav tests focus only on AuthorizeView rendering.
        ComponentFactories.AddStub<SearchBar>();
        ComponentFactories.AddStub<NotificationBell>();
    }

    [Fact]
    public void MainLayout_Unauthenticated_ShowsLoginHidesCollection()
    {
        Auth.SetNotAuthorized();

        var cut = RenderComponent<MainLayout>(p => p.Add(l => l.Body, "<div></div>"));

        Assert.Contains("Log In", cut.Markup);
        Assert.DoesNotContain("Your Collection", cut.Markup);
        Assert.DoesNotContain("href=\"/admin\"", cut.Markup);
    }

    [Fact]
    public void MainLayout_AuthenticatedNonAdmin_ShowsCollectionHidesAdmin()
    {
        Auth.SetAuthorized("user");

        var cut = RenderComponent<MainLayout>(p => p.Add(l => l.Body, "<div></div>"));

        cut.WaitForAssertion(() => Assert.Contains("Your Collection", cut.Markup));
        Assert.DoesNotContain("href=\"/admin\"", cut.Markup);
    }

    [Fact]
    public void MainLayout_Admin_ShowsAdminButtonHidesCollection()
    {
        Auth.SetAuthorized("admin");
        Auth.SetRoles("Admin");

        var cut = RenderComponent<MainLayout>(p => p.Add(l => l.Body, "<div></div>"));

        cut.WaitForAssertion(() => Assert.Contains("href=\"/admin\"", cut.Markup));
        Assert.DoesNotContain("Your Collection", cut.Markup);
    }
}
