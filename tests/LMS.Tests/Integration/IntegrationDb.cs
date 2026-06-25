using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Creates a throwaway PostgreSQL database for integration tests, applies the
/// migrations, and drops it on dispose. SELF-SKIPPING: if no Postgres is
/// reachable (e.g. CI without a DB) <see cref="Available"/> is false and the
/// tests Skip — so CI stays green and these run only where a DB exists.
///
/// Point it at a server with the EDUVIBE_TEST_PG env var (a connection string to
/// the maintenance/"postgres" database); defaults to the local dev Postgres.
/// </summary>
public sealed class IntegrationDb : IAsyncLifetime
{
    private const string DefaultAdmin =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private string _adminConn = "";
    private string _dbName = "";
    private string _testConn = "";

    public bool Available { get; private set; }
    public string SkipReason { get; private set; } = "";

    public async Task InitializeAsync()
    {
        _adminConn = Environment.GetEnvironmentVariable("EDUVIBE_TEST_PG") ?? DefaultAdmin;
        _dbName = "eduvibe_test_" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var admin = new NpgsqlConnection(_adminConn))
            {
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{_dbName}\"";
                await create.ExecuteNonQueryAsync();
            }
            _testConn = new NpgsqlConnectionStringBuilder(_adminConn) { Database = _dbName }.ConnectionString;
            await using var ctx = CreateContext();
            await ctx.Database.MigrateAsync();
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason = $"No Postgres reachable for integration tests ({ex.GetType().Name}: {ex.Message}).";
        }
    }

    public LMSDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<LMSDbContext>().UseNpgsql(_testConn).Options);

    public async Task DisposeAsync()
    {
        if (!Available) return;
        try
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(_adminConn);
            await admin.OpenAsync();
            await using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)";
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup; a leaked throwaway DB is harmless.
        }
    }
}
