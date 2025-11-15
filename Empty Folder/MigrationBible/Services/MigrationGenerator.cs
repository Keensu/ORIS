using MigrationLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Services
{
    public class MigrationGenerator
    {
        public (string UpSql, string DownSql) GenerateMigration(List<Table> currentSchema, List<Table> targetSchema)
        {
            var upBuilder = new StringBuilder();
            var downBuilder = new StringBuilder();

            var currentTables = currentSchema.ToDictionary(t => t.Name);
            var targetTables = targetSchema.ToDictionary(t => t.Name);

            // Удаленные таблицы (в текущей БД, но не в целевых моделях)
            foreach (var currentTable in currentSchema)
            {
                if (!targetTables.ContainsKey(currentTable.Name))
                {
                    upBuilder.AppendLine(GenerateDropTableSql(currentTable));
                    downBuilder.AppendLine(GenerateCreateTableSql(currentTable));
                    upBuilder.AppendLine();
                }
            }

            // Новые таблицы (в целевых моделях, но не в текущей БД)
            foreach (var targetTable in targetSchema)
            {
                if (!currentTables.ContainsKey(targetTable.Name))
                {
                    upBuilder.AppendLine(GenerateCreateTableSql(targetTable));
                    downBuilder.AppendLine(GenerateDropTableSql(targetTable));
                    upBuilder.AppendLine();
                }
            }

            // TODO: Здесь можно добавить логику для изменения существующих таблиц

            return (upBuilder.ToString().Trim(), downBuilder.ToString().Trim());
        }

        private string GenerateCreateTableSql(Table table)
        {
            var sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE {table.Name} (");

            var columns = new List<string>();
            var primaryKeys = new List<string>();

            foreach (var column in table.Columns)
            {
                var columnSql = $"{column.Name} {GetPostgreSqlType(column)}";

                if (!column.IsNullable)
                    columnSql += " NOT NULL";

                if (column.IsPrimaryKey)
                    primaryKeys.Add(column.Name);

                columns.Add(columnSql);
            }

            // Добавляем PRIMARY KEY constraint если есть первичные ключи
            if (primaryKeys.Any())
            {
                columns.Add($"PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }

            sql.AppendLine("    " + string.Join(",\n    ", columns));
            sql.AppendLine(");");

            return sql.ToString();
        }

        private string GenerateDropTableSql(Table table)
        {
            return $"DROP TABLE IF EXISTS {table.Name};";
        }

        private string GetPostgreSqlType(Column column)
        {
            return column.DataType.ToLower() switch
            {
                "int" => "INTEGER",
                "string" => "TEXT",
                _ => "TEXT"
            };
        }
    }
}
