using Blog.Domain.Entities;

namespace Blog.Application.Abstractions.Persistence;

public interface IPostRepository
{
    Task<IReadOnlyList<Post>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Post post, bool publish, CancellationToken cancellationToken = default);
    Task UpdateAsync(Post post, bool publish, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid postId, CancellationToken cancellationToken = default);
    Task SetPublicationStatusAsync(Guid postId, bool publish, CancellationToken cancellationToken = default);
}
