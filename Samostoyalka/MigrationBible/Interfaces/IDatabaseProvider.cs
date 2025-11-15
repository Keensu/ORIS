using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Interfaces
{
    public interface IDatabaseProvider
    {
        Task<bool> TestConnectionAsync();
        Task EnsureMigrationsTableExistsAsync();
        Task<List<Table>> GetCurrentSchemaAsync();
        Task<List<MigrationRec>> GetMigrationHistoryAsync();
        Task ApplyMigrationAsync(string upSql, string downSql, string migrationName, string modelSnapshot);
        Task RollbackMigrationAsync();
        Task<bool> MigrationTableExistsAsync();
    }
}
