using CentroDeProduccion.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Tests.Infrastructure;

/// <summary>
/// Creates throwaway SQL Server LocalDB databases for tests that need a real engine.
/// Every database-backed test uses the same engine the application runs on, so behaviour that
/// differs between providers (rowversion, collation, decimal precision) is actually covered.
/// </summary>
internal static class LocalDb
{
    private const string InstanceName = @"(localdb)\MSSQLLocalDB";

    /// <summary>
    /// Builds a context against its own database and creates the schema. The caller owns the
    /// context and must dispose it through <see cref="DropAsync"/> so nothing is left behind.
    /// </summary>
    internal static async Task<AppDbContext> CreateAsync(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionStringFor(databaseName))
            .Options;

        var db = new AppDbContext(options);

        try
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
        catch (SqlException ex)
        {
            await db.DisposeAsync();
            throw new InvalidOperationException(
                $"Could not reach SQL Server LocalDB instance '{InstanceName}'. This test is " +
                @"tagged [Trait(""Category"", ""SqlServer"")] and needs a real SQL Server engine. " +
                "Register the instance with `sqllocaldb create MSSQLLocalDB`, or skip these " +
                "tests with `dotnet test --filter Category!=SqlServer`.",
                ex);
        }

        return db;
    }

    /// <summary>Drops the test database and disposes the context.</summary>
    internal static async Task DropAsync(AppDbContext db)
    {
        await db.Database.EnsureDeletedAsync();
        await db.DisposeAsync();
    }

    /// <summary>
    /// Options that build the EF Core model without opening a connection. Use for tests that
    /// only inspect <see cref="DbContext.Model"/> — they need no database and no LocalDB.
    /// </summary>
    internal static DbContextOptions<AppDbContext> ModelOnlyOptions() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionStringFor("model-only-never-connected"))
            .Options;

    private static string ConnectionStringFor(string databaseName) =>
        $"Server={InstanceName};Database=CentroDeProduccion.Tests.{databaseName};" +
        "Trusted_Connection=true;TrustServerCertificate=true";
}
