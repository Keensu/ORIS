using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MigrationLib.Services;

namespace Migration
{
    public class MigrationServer
    {
        private readonly HttpListener _listener;
        private readonly MigrationService _migrationService;
        private readonly string _url;

        public MigrationServer(MigrationService migrationService, string url)
        {
            _migrationService = migrationService;
            _url = url;
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"Migration API Server started on {_url}");
            Console.WriteLine("Available endpoints:");
            Console.WriteLine("  GET /migrate/create");
            Console.WriteLine("  GET /migrate/apply");
            Console.WriteLine("  GET /migrate/rollback");
            Console.WriteLine("  GET /migrate/status");
            Console.WriteLine("  GET /migrate/log");

            while (true)
            {
                var context = await _listener.GetContextAsync();
                _ = ProcessRequestAsync(context);
            }
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("Server stopped");
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {request.HttpMethod} {request.Url.AbsolutePath}");

                var result = await HandleRequestAsync(request);
                await WriteResponseAsync(response, 200, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await WriteErrorResponseAsync(response, 500, ex.Message);
            }
        }

        private async Task<object> HandleRequestAsync(HttpListenerRequest request)
        {
            var path = request.Url.AbsolutePath;
            var method = request.HttpMethod;

            if (method != "GET")
            {
                throw new Exception("Method not allowed");
            }

            switch (path)
            {
                case "/migrate/create":
                    return await HandleCreateMigrationAsync();
                case "/migrate/apply":
                    return await HandleApplyMigrationAsync();
                case "/migrate/rollback":
                    return await HandleRollbackMigrationAsync();
                case "/migrate/status":
                    return await HandleStatusAsync();
                case "/migrate/log":
                    return await HandleLogAsync();
                case "/":
                    return new
                    {
                        message = "Migration API Server is running",
                        endpoints = new[] {
                        "/migrate/create", "/migrate/apply", "/migrate/rollback", "/migrate/status", "/migrate/log"
                    }
                    };
                default:
                    throw new Exception("Endpoint not found");
            }
        }

        private async Task<object> HandleCreateMigrationAsync()
        {
            try
            {
                var migrationName = await _migrationService.CreateMigrationAsync();
                return new
                {
                    migration = migrationName,
                    status = "created",
                    message = "Migration SQL generated successfully"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create migration: {ex.Message}");
            }
        }

        private async Task<object> HandleApplyMigrationAsync()
        {
            try
            {
                var migrationName = await _migrationService.ApplyMigrationAsync();
                return new
                {
                    migration = migrationName,
                    status = "applied",
                    message = "Migration applied successfully"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to apply migration: {ex.Message}");
            }
        }

        private async Task<object> HandleRollbackMigrationAsync()
        {
            try
            {
                var result = await _migrationService.RollbackMigrationAsync();
                return new
                {
                    migration = result,
                    status = "rolled_back",
                    message = "Migration rolled back successfully"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to rollback migration: {ex.Message}");
            }
        }

        private async Task<object> HandleStatusAsync()
        {
            try
            {
                var status = await _migrationService.GetStatusAsync();

                return new
                {
                    has_pending_changes = status.HasPendingChanges,
                    current_schema_tables = status.CurrentSchema.Count,
                    target_schema_tables = status.TargetSchema.Count,
                    migration_count = status.MigrationHistory.Count,
                    details = status.HasPendingChanges ?
                        "Database schema differs from models" :
                        "Database schema is up to date"
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get status: {ex.Message}");
            }
        }

        private async Task<object> HandleLogAsync()
        {
            try
            {
                var status = await _migrationService.GetStatusAsync();

                return new
                {
                    total_migrations = status.MigrationHistory.Count,
                    migrations = status.MigrationHistory.Select(m => new {
                        id = m.Id,
                        name = m.MigrationName,
                        applied_at = m.AppliedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get migration log: {ex.Message}");
            }
        }

        private async Task WriteResponseAsync(HttpListenerResponse response, int statusCode, object data)
        {
            response.ContentType = "application/json";
            response.StatusCode = statusCode;

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private async Task WriteErrorResponseAsync(HttpListenerResponse response, int statusCode, string errorMessage)
        {
            response.ContentType = "application/json";
            response.StatusCode = statusCode;

            var errorResponse = new { error = errorMessage };
            var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
    }
}
