namespace Blog.Domain.Entities;

public class Post
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public bool IsPublished { get; set; }
}

