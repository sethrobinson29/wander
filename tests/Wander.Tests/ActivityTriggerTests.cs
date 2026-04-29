using Wander.Api.Domain;
using Wander.Api.Services;

namespace Wander.Tests;

public class ActivityTriggerTests
{
    [Theory]
    [InlineData(Visibility.Private,  Visibility.Public,   true)]   // private → public:   fires
    [InlineData(Visibility.Unlisted, Visibility.Public,   true)]   // unlisted → public:  fires
    [InlineData(Visibility.Public,   Visibility.Public,   false)]  // public → public:    no-op
    [InlineData(Visibility.Public,   Visibility.Private,  false)]  // public → private:   no-op
    [InlineData(Visibility.Private,  Visibility.Unlisted, false)]  // private → unlisted: no-op
    public void IsMadePublic_FiredCorrectly(Visibility prev, Visibility next, bool expected) =>
        Assert.Equal(expected, ActivityService.IsMadePublic(prev, next));
}
