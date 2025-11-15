using MigrationLib.Interfaces;
using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MigrationLib.Services
{
    public class MigrationService
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly IModelScanner _modelScanner;
        private readonly MigrationGenerator _migrationGenerator;

        private (string UpSql, string DownSql) _pendingMigration;
        private string _pendingMigrationName;

        public MigrationService(IDatabaseProvider databaseProvider, IModelScanner modelScanner)
        {
            _databaseProvider = databaseProvider;
            _modelScanner = modelScanner;
            _migrationGenerator = new MigrationGenerator();
        }

        public async Task<string> CreateMigrationAsync()
        {
            await _databaseProvider.EnsureMigrationsTableExistsAsync();

            var currentSchema = await _databaseProvider.GetCurrentSchemaAsync();
            var targetSchema = _modelScanner.ScanModels();

            _pendingMigration = _migrationGenerator.GenerateMigration(currentSchema, targetSchema);
            _pendingMigrationName = $"Migration_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            Console.WriteLine($"Created migration: {_pendingMigrationName}");
            Console.WriteLine($"Up SQL: {_pendingMigration.UpSql}");
            Console.WriteLine($"Down SQL: {_pendingMigration.DownSql}");

            return _pendingMigrationName;
        }

        public async Task<string> ApplyMigrationAsync()
        {
            if (string.IsNullOrEmpty(_pendingMigrationName) || string.IsNullOrEmpty(_pendingMigration.UpSql))
                throw new InvalidOperationException("No pending migration to apply");

            var targetSchema = _modelScanner.ScanModels();
            var snapshot = JsonSerializer.Serialize(targetSchema);

            await _databaseProvider.ApplyMigrationAsync(
                _pendingMigration.UpSql,
                _pendingMigration.DownSql,
                _pendingMigrationName,
                snapshot);

            var appliedName = _pendingMigrationName;
            _pendingMigrationName = null;
            _pendingMigration = (null, null);

            return appliedName;
        }

        public async Task<string> RollbackMigrationAsync()
        {
            await _databaseProvider.RollbackMigrationAsync();
            return "Last migration rolled back";
        }

        public async Task<MigrationStatus> GetStatusAsync()
        {
            var currentSchema = await _databaseProvider.GetCurrentSchemaAsync();
            var targetSchema = _modelScanner.ScanModels();
            var history = await _databaseProvider.GetMigrationHistoryAsync();

            var hasChanges = !AreSchemasEqual(currentSchema, targetSchema);

            return new MigrationStatus
            {
                HasPendingChanges = hasChanges,
                CurrentSchema = currentSchema,
                TargetSchema = targetSchema,
                MigrationHistory = history
            };
        }

        private bool AreSchemasEqual(System.Collections.Generic.List<Table> schema1, System.Collections.Generic.List<Table> schema2)
        {
            if (schema1.Count != schema2.Count)
                return false;

            for (int i = 0; i < schema1.Count; i++)
            {
                var table1 = schema1[i];
                var table2 = schema2[i];

                if (table1.Name != table2.Name || table1.Columns.Count != table2.Columns.Count)
                    return false;

                for (int j = 0; j < table1.Columns.Count; j++)
                {
                    var col1 = table1.Columns[j];
                    var col2 = table2.Columns[j];

                    if (col1.Name != col2.Name || col1.DataType != col2.DataType ||
                        col1.IsPrimaryKey != col2.IsPrimaryKey || col1.IsNullable != col2.IsNullable)
                        return false;
                }
            }

            return true;
        }
    }
}
