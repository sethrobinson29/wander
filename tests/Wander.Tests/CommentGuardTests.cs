using Wander.Api.Domain;
using Wander.Api.Services;

namespace Wander.Tests;

public class CommentGuardTests
{
    [Fact]
    public void TopLevelComment_IsTopLevel()
    {
        var comment = new DeckComment
        {
            Id = Guid.NewGuid(),
            AuthorId = "u1",
            Body = "Top level",
            ParentCommentId = null
        };
        Assert.True(CommentGuard.IsTopLevel(comment));
    }

    [Fact]
    public void Reply_IsNotTopLevel()
    {
        var comment = new DeckComment
        {
            Id = Guid.NewGuid(),
            AuthorId = "u1",
            Body = "I am a reply",
            ParentCommentId = Guid.NewGuid()
        };
        Assert.False(CommentGuard.IsTopLevel(comment));
    }
}
