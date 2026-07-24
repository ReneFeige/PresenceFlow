using Microsoft.EntityFrameworkCore;

namespace PresenceFlow.Data
{
    /// <summary>
    /// Erweiterungsmethoden für die Datenbankinitialisierung.
    /// </summary>
    public static class DatabaseInitializationExtensions
    {
        /// <summary>
        /// Initialisiert die Presence-Datenbank, falls SQLite genutzt wird.
        /// </summary>
        public static async Task InitializePresenceDatabaseAsync(this WebApplication app, string storageProvider)
        {
            // Nur für SQLite-Datenbanken ausführen
            if (!storageProvider.Equals("SQLite", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // DI-Scope für kurzlebige Services (DbContext) erstellen
            using var scope = app.Services.CreateScope();

            // Datenbankkontext auflösen
            var dbContext = scope.ServiceProvider.GetRequiredService<PresenceDbContext>();

            // Ausstehende EF-Migrationen anwenden (erstellt auch die DB)
            await dbContext.Database.MigrateAsync();

            // Testdaten einpflegen
            await PresenceDbSeeder.SeedAsync(dbContext);
        }
    }

}
