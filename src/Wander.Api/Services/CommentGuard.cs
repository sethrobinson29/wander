using Wander.Api.Domain;

namespace Wander.Api.Services;

internal static class CommentGuard
{
    /// <summary>Returns true when the comment can be replied to (is itself a top-level comment).</summary>
    public static bool IsTopLevel(DeckComment comment) =>
        comment.ParentCommentId is null;
}
