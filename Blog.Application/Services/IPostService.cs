using Blog.Application.Contracts.Posts;

namespace Blog.Application.Services;

public interface IPostService
{
    Task<IReadOnlyList<PostDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<PostDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreatePostRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdatePostRequest request, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid postId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid postId, CancellationToken cancellationToken = default);
}
