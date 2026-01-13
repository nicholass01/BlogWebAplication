namespace Blog.Application.Contracts.Posts;

public record PostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Title,
    string Slug,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    bool IsPublished);

