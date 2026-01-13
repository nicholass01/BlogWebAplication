using System.Data;
using Blog.Application.Abstractions.Persistence;
using Blog.Domain.Entities;
using Dapper;
using Blog.Infrastructure.Data;

namespace Blog.Infrastructure.Repositories;

public class PostgresUserRepository : IUserRepository
{
    private readonly INpgsqlConnectionFactory _connectionFactory;

    public PostgresUserRepository(INpgsqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Banco atual não expõe função por username; consulta direta na tabela.
        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            commandText: @"
                select 
                    id,
                    name as username,
                    email,
                    password_hash as passwordhash,
                    created_at as CreatedAtUtc
                from blog.users
                where name = @p_username
                limit 1;",
            parameters: new { p_username = username },
            cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            commandText: @"
                select 
                    id,
                    name as username,
                    email,
                    password_hash as passwordhash,
                    created_at as CreatedAtUtc
                from blog.fn_get_user_by_email(@p_email);",
            parameters: new { p_email = email },
            cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("p_name", user.Username);
        parameters.Add("p_email", user.Email);
        parameters.Add("p_password_hash", user.PasswordHash);
        parameters.Add("p_user_id", dbType: DbType.Guid, direction: ParameterDirection.Output);

        await connection.ExecuteAsync(new CommandDefinition(
            commandText: "blog.sp_create_user",
            commandType: CommandType.StoredProcedure,
            parameters: parameters,
            cancellationToken: cancellationToken));

        var id = parameters.Get<Guid>("p_user_id");
        return id;
    }
}
