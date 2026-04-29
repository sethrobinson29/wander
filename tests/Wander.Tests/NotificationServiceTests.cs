using Wander.Api.Services;

namespace Wander.Tests;

public class NotificationServiceTests
{
    [Fact]
    public void SameUser_ShouldNotNotify()
    {
        Assert.False(NotificationService.ShouldNotify("user-1", "user-1"));
    }

    [Fact]
    public void DifferentUsers_ShouldNotify()
    {
        Assert.True(NotificationService.ShouldNotify("user-1", "user-2"));
    }
}
