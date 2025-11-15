using MigrationLib.Interfaces;
using MigrationLib.Models;
using MigrationLib.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MigrationLib.Services
{
    public class ModelScanner : IModelScanner
    {
        public List<Table> ScanModels()
        {
            var tables = new List<Table>();

            try
            {
                // Сканируем текущую сборку на наличие классов с атрибутом [Table]
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null)
                {
                    Console.WriteLine("No entry assembly found");
                    return tables;
                }

                var types = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<TableAttribute>() != null);

                Console.WriteLine($"Found {types.Count()} types with [Table] attribute");

                foreach (var type in types)
                {
                    var tableAttr = type.GetCustomAttribute<TableAttribute>();
                    var table = new Table { Name = tableAttr.Name };

                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in properties)
                    {
                        // Поддерживаем только int и string
                        if (prop.PropertyType != typeof(int) && prop.PropertyType != typeof(string))
                            continue;

                        var isPrimaryKey = prop.GetCustomAttribute<PrimaryKeyAttribute>() != null;
                        var columnAttribute = prop.GetCustomAttribute<ColumnAttribute>();

                        var column = new Column
                        {
                            Name = columnAttribute?.Name ?? prop.Name.ToLower(),
                            DataType = prop.PropertyType.Name.ToLower(),
                            IsPrimaryKey = isPrimaryKey,
                            IsNullable = prop.PropertyType == typeof(string) // string nullable по умолчанию
                        };

                        table.Columns.Add(column);
                    }

                    if (table.Columns.Any())
                    {
                        tables.Add(table);
                        Console.WriteLine($"Found table: {table.Name} with {table.Columns.Count} columns");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning models: {ex.Message}");
            }

            return tables;
        }
    }
}
