using Migration;
using MigrationLib.Interfaces;
using MigrationLib.Services;
using MigrationLib;
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Конфигурация PostgreSQL
            var connectionString = "Host=localhost;Database=migration;Username=postgres;Password=g7-gh-c5hc;";
            var databaseProvider = new PostgreSqlProvider(connectionString);
            var modelScanner = new ModelScanner();

            var migrationService = new MigrationService(databaseProvider, modelScanner);
            var apiServer = new MigrationServer(migrationService, "http://localhost:9000/");

            Console.WriteLine("Starting Migration API Server...");
            await apiServer.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start server: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}