namespace Wander.Api.Models.Decks;

public record CommentResponse(
    Guid Id,
    string AuthorId,
    string AuthorUsername,
    string? AuthorAvatarId,
    string Body,
    bool IsDeleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    List<CommentResponse> Replies);

public record PostCommentRequest(string Body);
