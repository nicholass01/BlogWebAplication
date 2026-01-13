using System.Data;
using Blog.Application.Abstractions.Persistence;
using Blog.Domain.Entities;
using Dapper;
using Blog.Infrastructure.Data;

namespace Blog.Infrastructure.Repositories;

public class PostgresPostRepository : IPostRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresPostRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Post>> ListPublishedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var posts = await connection.QueryAsync<Post>(new CommandDefinition(
            commandText: @"
                select 
                    p.id,
                    p.author_id as AuthorId,
                    u.name as AuthorName,
                    p.title,
                    p.slug,
                    p.content,
                    coalesce(p.created_at_utc, p.created_at) as CreatedAtUtc,
                    coalesce(p.updated_at_utc, p.updated_at) as UpdatedAtUtc,
                    coalesce(p.published_at_utc, p.published_at) as PublishedAtUtc,
                    p.is_published as IsPublished
                from blog.list_published_posts() p\n                join blog.users u on u.id = p.author_id\n                order by coalesce(p.published_at_utc, p.published_at, p.created_at_utc, p.created_at) desc;",
            cancellationToken: cancellationToken));

        return posts.ToList();
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // NÃ£o hÃ¡ function especÃ­fica; consulta direta na tabela.
        return await connection.QuerySingleOrDefaultAsync<Post>(new CommandDefinition(
                        commandText: @"
                select 
                    p.id,
                    p.author_id as AuthorId,
                    u.name as AuthorName,
                    p.title,
                    p.slug,
                    p.content,
                    coalesce(p.created_at_utc, p.created_at) as CreatedAtUtc,
                    coalesce(p.updated_at_utc, p.updated_at) as UpdatedAtUtc,
                    coalesce(p.published_at_utc, p.published_at) as PublishedAtUtc,
                    p.is_published as IsPublished
                from blog.get_post_by_id(@p_id) p\n                join blog.users u on u.id = p.author_id;",
            parameters: new { p_id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<Post?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Post>(new CommandDefinition(
            commandText: @"
                select 
                    p.id,
                    p.author_id as AuthorId,
                    u.name as AuthorName,
                    p.title,
                    p.slug,
                    p.content,
                    p.is_published as IsPublished,
                    coalesce(p.published_at_utc, p.published_at) as PublishedAtUtc,
                    coalesce(p.created_at_utc, p.created_at) as CreatedAtUtc,
                    coalesce(p.updated_at_utc, p.updated_at) as UpdatedAtUtc
                from blog.get_post_by_slug(@p_slug) p\n                join blog.users u on u.id = p.author_id;",
            parameters: new { p_slug = slug },
            cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateAsync(Post post, bool publish, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("p_author_id", post.AuthorId);
        parameters.Add("p_title", post.Title);
        parameters.Add("p_slug", post.Slug);
        parameters.Add("p_content", post.Content);
        parameters.Add("p_post_id", dbType: DbType.Guid, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(new CommandDefinition(
            commandText: "blog.sp_create_post",
            commandType: CommandType.StoredProcedure,
            parameters: parameters,
            cancellationToken: cancellationToken));

        var createdId = parameters.Get<Guid>("p_post_id");

        if (publish)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                commandText: "blog.sp_publish_post",
                commandType: CommandType.StoredProcedure,
                parameters: new { p_post_id = createdId },
                cancellationToken: cancellationToken));
        }

        return createdId;
    }

    public async Task UpdateAsync(Post post, bool publish, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("p_post_id", post.Id);
        parameters.Add("p_title", post.Title);
        parameters.Add("p_content", post.Content);

        await connection.ExecuteAsync(new CommandDefinition(
            commandText: "blog.sp_update_post",
            commandType: CommandType.StoredProcedure,
            parameters: parameters,
            cancellationToken: cancellationToken));

        if (publish)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                commandText: "blog.sp_publish_post",
                commandType: CommandType.StoredProcedure,
                parameters: new { p_post_id = post.Id },
                cancellationToken: cancellationToken));
        }
    }

    public async Task DeleteAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            commandText: "blog.sp_delete_post",
            commandType: CommandType.StoredProcedure,
            parameters: new { p_post_id = postId },
            cancellationToken: cancellationToken));
    }

    public async Task SetPublicationStatusAsync(Guid postId, bool publish, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (publish)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                commandText: "blog.sp_publish_post",
                commandType: CommandType.StoredProcedure,
                parameters: new { p_post_id = postId },
                cancellationToken: cancellationToken));
        }
        else
        {
            // Banco nÃ£o expÃµe "despublicar"; nesse caso nÃ£o faz nada.
        }
    }
}



