using Npgsql;

namespace Blog.Infrastructure.Data;

public interface INpgsqlConnectionFactory
{
    Task<NpgsqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
