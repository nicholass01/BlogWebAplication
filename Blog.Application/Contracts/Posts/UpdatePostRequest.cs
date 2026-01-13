namespace Blog.Application.Contracts.Posts;

public record UpdatePostRequest(
    Guid PostId,
    Guid AuthorId,
    string Title,
    string Content,
    bool Publish = false);
