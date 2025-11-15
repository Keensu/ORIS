using MigrationLib.Interfaces;
using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MigrationLib
{
    public class PostgreSqlProvider : IDatabaseProvider
    {
        private readonly string _connectionString;

        public PostgreSqlProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MigrationTableExistsAsync()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                var command = new NpgsqlCommand(@"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND table_name = '_migrations'
                )", connection);

                var result = await command.ExecuteScalarAsync();
                return (bool)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Check migration table failed: {ex.Message}");
                return false;
            }
        }

        public async Task EnsureMigrationsTableExistsAsync()
        {
            if (await MigrationTableExistsAsync())
                return;

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var createTableSql = @"
            CREATE TABLE _migrations (
                id SERIAL PRIMARY KEY,
                migration_name TEXT NOT NULL,
                applied_at TIMESTAMP NOT NULL DEFAULT NOW(),
                model_snapshot TEXT NOT NULL,
                up_sql TEXT NOT NULL,
                down_sql TEXT NOT NULL
            )";

            using var command = new NpgsqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();

            Console.WriteLine("Created _migrations table");
        }

        public async Task<List<Table>> GetCurrentSchemaAsync()
        {
            var tables = new List<Table>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            // Получаем все таблицы (кроме _migrations)
            var tablesCommand = new NpgsqlCommand(@"
            SELECT table_name 
            FROM information_schema.tables 
            WHERE table_schema = 'public' 
            AND table_type = 'BASE TABLE'
            AND table_name != '_migrations'
            ORDER BY table_name", connection);

            using var tablesReader = await tablesCommand.ExecuteReaderAsync();
            var tableNames = new List<string>();

            while (await tablesReader.ReadAsync())
            {
                tableNames.Add(tablesReader.GetString(0));
            }
            await tablesReader.CloseAsync();

            // Для каждой таблицы получаем колонки
            foreach (var tableName in tableNames)
            {
                var table = new Table { Name = tableName };

                var columnsCommand = new NpgsqlCommand(@"
                SELECT 
                    column_name,
                    data_type,
                    is_nullable,
                    CASE WHEN position('nextval' in column_default) > 0 THEN true ELSE false END as is_identity,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku 
                    ON tc.constraint_name = ku.constraint_name
                    AND ku.table_schema = tc.table_schema
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                    AND ku.table_name = @tableName
                ) pk ON c.column_name = pk.column_name
                WHERE c.table_name = @tableName
                ORDER BY c.ordinal_position", connection);

                columnsCommand.Parameters.AddWithValue("@tableName", tableName);

                using var columnsReader = await columnsCommand.ExecuteReaderAsync();
                while (await columnsReader.ReadAsync())
                {
                    var columnName = columnsReader.GetString(0);
                    var dataType = columnsReader.GetString(1);
                    var isNullable = columnsReader.GetString(2) == "YES";
                    var isIdentity = columnsReader.GetBoolean(3);
                    var isPrimaryKey = columnsReader.GetBoolean(4);

                    table.Columns.Add(new Column
                    {
                        Name = columnName,
                        DataType = MapDataType(dataType),
                        IsNullable = isNullable,
                        IsPrimaryKey = isPrimaryKey
                    });
                }

                if (table.Columns.Any())
                    tables.Add(table);
            }

            Console.WriteLine($"Retrieved current schema: {tables.Count} tables");
            return tables;
        }

        private string MapDataType(string postgresType)
        {
            return postgresType.ToLower() switch
            {
                "integer" or "int" or "int4" or "serial" => "int",
                "text" or "character varying" or "varchar" => "string",
                _ => "string"
            };
        }

        public async Task<List<MigrationRec>> GetMigrationHistoryAsync()
        {
            var migrations = new List<MigrationRec>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var command = new NpgsqlCommand(@"
            SELECT id, migration_name, applied_at, model_snapshot, up_sql, down_sql
            FROM _migrations 
            ORDER BY applied_at DESC", connection);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                migrations.Add(new MigrationRec
                {
                    Id = reader.GetInt32(0),
                    MigrationName = reader.GetString(1),
                    AppliedAt = reader.GetDateTime(2),
                    ModelSnapshot = reader.GetString(3),
                    UpSql = reader.GetString(4),
                    DownSql = reader.GetString(5)
                });
            }

            Console.WriteLine($"Retrieved migration history: {migrations.Count} migrations");
            return migrations;
        }

        public async Task ApplyMigrationAsync(string upSql, string downSql, string migrationName, string modelSnapshot)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                Console.WriteLine($"Applying migration: {migrationName}");

                // Разделяем SQL на отдельные команды
                var sqlCommands = upSql.Split(';')
                    .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                    .Select(cmd => cmd.Trim())
                    .Where(cmd => !string.IsNullOrEmpty(cmd));

                // Выполняем каждую команду
                foreach (var sqlCommand in sqlCommands)
                {
                    Console.WriteLine($"Executing: {sqlCommand}");
                    using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                }

                // Сохраняем в историю миграций
                var insertCommand = new NpgsqlCommand(@"
                INSERT INTO _migrations (migration_name, model_snapshot, up_sql, down_sql)
                VALUES (@name, @snapshot, @up, @down)", connection, transaction);

                insertCommand.Parameters.AddWithValue("@name", migrationName);
                insertCommand.Parameters.AddWithValue("@snapshot", modelSnapshot);
                insertCommand.Parameters.AddWithValue("@up", upSql);
                insertCommand.Parameters.AddWithValue("@down", downSql);

                await insertCommand.ExecuteNonQueryAsync();
                await transaction.CommitAsync();

                Console.WriteLine($"Migration {migrationName} applied successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Migration failed: {ex.Message}", ex);
            }
        }

        public async Task RollbackMigrationAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Получаем последнюю миграцию
                var getLastCommand = new NpgsqlCommand(@"
                SELECT id, down_sql, migration_name FROM _migrations 
                ORDER BY applied_at DESC LIMIT 1", connection, transaction);

                using var reader = await getLastCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    await reader.CloseAsync();
                    throw new InvalidOperationException("No migrations to rollback");
                }

                var migrationId = reader.GetInt32(0);
                var downSql = reader.GetString(1);
                var migrationName = reader.GetString(2);
                await reader.CloseAsync();

                Console.WriteLine($"Rolling back migration: {migrationName}");

                // Выполняем откат
                var sqlCommands = downSql.Split(';')
                    .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                    .Select(cmd => cmd.Trim())
                    .Where(cmd => !string.IsNullOrEmpty(cmd));

                foreach (var sqlCommand in sqlCommands)
                {
                    Console.WriteLine($"Executing rollback: {sqlCommand}");
                    using var command = new NpgsqlCommand(sqlCommand, connection, transaction);
                    await command.ExecuteNonQueryAsync();
                }

                // Удаляем запись миграции
                var deleteCommand = new NpgsqlCommand(
                    "DELETE FROM _migrations WHERE id = @id",
                    connection, transaction);
                deleteCommand.Parameters.AddWithValue("@id", migrationId);
                await deleteCommand.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                Console.WriteLine($"Migration {migrationName} rolled back successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Rollback failed: {ex.Message}", ex);
            }
        }
    }
}
