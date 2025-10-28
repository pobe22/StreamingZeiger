using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Models;
using System.Data;
using System.Text;
using StreamingZeiger.Data;

namespace StreamingZeiger.Services
{
    /// <summary>
    /// Führt Backup-Operationen für die SQLite-Datenbank aus:
    /// - Verbindungen schließen (Cache leeren)
    /// - Tabelle Movies exklusiv sperren
    /// - CSV-Export in "back12.txt"
    /// - Volle Sicherung in "dump12.sqlite"
    /// </summary>
    public class DatabaseBackupService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseBackupService> _logger;

        public DatabaseBackupService(AppDbContext context, ILogger<DatabaseBackupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Führt die komplette Sicherung analog zu den MySQL-Schritten aus.
        /// </summary>
        public async Task PerformBackupAsync()
        {
            string csvPath = "back12.txt";
            string sqliteBackupPath = "dump12.sqlite";

            try
            {
                _logger.LogInformation("Beginne Datenbanksicherung...");

                // 1️. Cache / Verbindung schließen
                await _context.Database.CloseConnectionAsync();
                _logger.LogInformation("Verbindungen geschlossen (Cache geleert).");

                // 2️. Tabelle "Movies" sperren (transaktionaler Schreibschutz)
                using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                _logger.LogInformation("Transaktion gestartet (Tabelle gesperrt).");

                // 3️. Exportiere Movies in ASCII-Datei
                await ExportMoviesToCsvAsync(csvPath);
                _logger.LogInformation("Tabelle Movies nach {CsvPath} exportiert.", csvPath);

                // 4. Komplette DB sichern (mysqldump-Äquivalent)
                await BackupSqliteDatabaseAsync(sqliteBackupPath);
                _logger.LogInformation("SQLite-Datenbank gesichert nach {SqliteBackupPath}.", sqliteBackupPath);

                _logger.LogInformation("Sicherung erfolgreich abgeschlossen.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei der Datenbanksicherung");
                throw;
            }
        }

        /// <summary>
        /// Exportiert alle Filme als CSV in eine Datei mit Anführungszeichen und Semikolons.
        /// </summary>
        private async Task ExportMoviesToCsvAsync(string filePath)
        {
            await using var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM MediaItems WHERE MediaType = 'Movie'";

            await using var reader = await command.ExecuteReaderAsync();

            await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

            int columnCount = reader.FieldCount;

            // Kopfzeile
            for (int i = 0; i < columnCount; i++)
            {
                if (i > 0) writer.Write(';');
                writer.Write($"\"{reader.GetName(i)}\"");
            }
            await writer.WriteLineAsync();

            // Datenzeilen
            while (await reader.ReadAsync())
            {
                for (int i = 0; i < columnCount; i++)
                {
                    if (i > 0) writer.Write(';');
                    var value = reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString()?.Replace("\"", "\"\"");
                    writer.Write($"\"{value}\"");
                }
                await writer.WriteLineAsync();
            }
        }

        /// <summary>
        /// Erstellt eine Kopie der SQLite-Datenbankdatei (mysqldump-Äquivalent).
        /// </summary>
        private async Task BackupSqliteDatabaseAsync(string backupFile)
        {
            await _context.Database.CloseConnectionAsync(); // sicherstellen, dass DB geschlossen ist

            var dbPath = GetDatabasePath();

            if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
                throw new FileNotFoundException("Datenbankdatei nicht gefunden.", dbPath);

            File.Copy(dbPath, backupFile, overwrite: true);
        }

        /// <summary>
        /// Liest den Dateipfad der SQLite-Datenbank aus der Connection-String.
        /// </summary>
        private string? GetDatabasePath()
        {
            var connStr = _context.Database.GetConnectionString();
            if (connStr == null) return null;

            // Connection string enthält: Data Source=app.db
            var prefix = "Data Source=";
            var start = connStr.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;

            var path = connStr.Substring(start + prefix.Length);
            return path.Split(';').FirstOrDefault();
        }
    }
}
