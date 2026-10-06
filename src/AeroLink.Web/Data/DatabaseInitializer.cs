using Microsoft.EntityFrameworkCore;

namespace AeroLink.Web.Data;

/// <summary>
/// Creates the SQLite database and seed data when the app starts.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Ensures the database exists and contains the manifest seed data.
    /// When <c>Database:ResetOnStartup</c> is true in configuration, the database is deleted first,
    /// which gives a clean dataset for demos.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="configuration">Application configuration.</param>
    public static void Initialise(IServiceProvider services, IConfiguration configuration)
    {
        using var serviceScope = services.CreateScope();
        var databaseContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (configuration.GetValue<bool>("Database:ResetOnStartup"))
        {
            // Start with a clean database when preparing a demo.
            databaseContext.Database.EnsureDeleted();
        }

        // Create the schema and add the HasData rows if the database does not exist yet.
        // After changing an entity, reset or delete aerolink.db so the schema can be rebuilt.
        databaseContext.Database.EnsureCreated();
    }
}
