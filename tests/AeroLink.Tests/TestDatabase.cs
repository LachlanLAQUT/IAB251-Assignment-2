using AeroLink.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AeroLink.Tests;

/// <summary>
/// An in-memory SQLite database with the manifest seed data for one test.
/// SQLite is used so unique indexes and foreign keys behave like they do in the app.
/// <example>
/// <code>
/// using var testDatabase = new TestDatabase();
/// using var databaseContext = testDatabase.CreateContext();
/// var service = new ManifestService(databaseContext);
/// </code>
/// </example>
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _databaseConnection;
    private readonly DbContextOptions<AppDbContext> _databaseOptions;

    /// <summary>Opens the database and creates its schema and seed data.</summary>
    public TestDatabase()
    {
        // The in-memory database lives only while this connection stays open.
        _databaseConnection = new SqliteConnection("DataSource=:memory:");
        _databaseConnection.Open();

        _databaseOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_databaseConnection)
            .Options;

        using var databaseContext = new AppDbContext(_databaseOptions);
        databaseContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Creates a context for the shared database. A second context can check that changes were
    /// saved to SQLite instead of only being tracked in memory.
    /// </summary>
    /// <returns>A new <see cref="AppDbContext"/>. Dispose it after use.</returns>
    public AppDbContext CreateContext() => new(_databaseOptions);

    /// <summary>Closes the connection, which deletes the in-memory database.</summary>
    public void Dispose() => _databaseConnection.Dispose();
}
