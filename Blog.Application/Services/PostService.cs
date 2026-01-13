using Blog.Application.Abstractions.Persistence;
using Blog.Application.Contracts.Posts;
using Blog.Domain.Entities;
using FluentValidation;

namespace Blog.Application.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _repository;
    private readonly IValidator<CreatePostRequest> _createValidator;
    private readonly IValidator<UpdatePostRequest> _updateValidator;

    public PostService(
        IPostRepository repository,
        IValidator<CreatePostRequest> createValidator,
        IValidator<UpdatePostRequest> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<PostDto>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _repository.ListPublishedAsync(cancellationToken);
        return posts.Select(MapToDto).ToList();
    }

    public async Task<PostDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var post = await _repository.GetBySlugAsync(slug, cancellationToken);
        return post is null ? null : MapToDto(post);
    }

    public async Task<Guid> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = request.AuthorId,
            Title = request.Title.Trim(),
            Slug = SlugGenerator.FromTitle(request.Title),
            Content = request.Content,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PublishedAtUtc = request.Publish ? now : null,
            IsPublished = request.Publish
        };

        return await _repository.CreateAsync(post, request.Publish, cancellationToken);
    }

    public async Task UpdateAsync(UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var existing = await _repository.GetByIdAsync(request.PostId, cancellationToken);
        if (existing is null)
        {
            throw new KeyNotFoundException($"Post {request.PostId} not found.");
        }

        existing.Title = request.Title.Trim();
        existing.Content = request.Content;
        existing.Slug = SlugGenerator.FromTitle(request.Title);
        existing.UpdatedAtUtc = DateTime.UtcNow;
        existing.PublishedAtUtc = request.Publish
            ? existing.PublishedAtUtc ?? existing.UpdatedAtUtc
            : existing.PublishedAtUtc;
        existing.IsPublished = request.Publish || existing.IsPublished;

        await _repository.UpdateAsync(existing, request.Publish, cancellationToken);
    }

    public Task PublishAsync(Guid postId, CancellationToken cancellationToken = default)
        => _repository.SetPublicationStatusAsync(postId, publish: true, cancellationToken);

    public Task DeleteAsync(Guid postId, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(postId, cancellationToken);

    private static PostDto MapToDto(Post post) =>
        new(
            post.Id,
            post.AuthorId,
            post.AuthorName,
            post.Title,
            post.Slug,
            post.Content,
            post.CreatedAtUtc,
            post.PublishedAtUtc,
            post.IsPublished);
}

