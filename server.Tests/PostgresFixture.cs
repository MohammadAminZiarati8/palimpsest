using Testcontainers.PostgreSql;

namespace Game.Server.Tests;

/// <summary>Real Postgres in Docker (D-63). If Docker is not running, tests that need it are skipped.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _db;

    public bool Available => _db is not null;
    public string ConnectionString => _db?.GetConnectionString() ?? "";

    public async ValueTask InitializeAsync()
    {
        try
        {
            var db = new PostgreSqlBuilder("postgres:18").Build();
            await db.StartAsync();
            _db = db;
        }
        catch (Exception)
        {
            _db = null; // Docker missing or not running.
        }
    }

    public ValueTask DisposeAsync() => _db?.DisposeAsync() ?? ValueTask.CompletedTask;
}
