namespace Blog.Application.Contracts.Posts;

public record CreatePostRequest(
    Guid AuthorId,
    string Title,
    string Content,
    bool Publish = false);
