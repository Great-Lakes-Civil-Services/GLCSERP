using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CivilProcessERP.Models;
using Npgsql;

namespace CivilProcessERP.Services
{
    public class AuditLogService
    {
        private readonly string _connectionString;

        public AuditLogService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task LogActionAsync(string username, string action, string detail, string changedBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_activity_log (username, action, detail, changed_by) 
                    VALUES (@u, @a, @d, @c)", conn);

                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@a", action);
                cmd.Parameters.AddWithValue("@d", detail);
                cmd.Parameters.AddWithValue("@c", changedBy);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to log action: {ex.Message}");
                throw;
            }
        }

        public async Task<List<UserActivityLog>> GetLogsAsync()
        {
            var logs = new List<UserActivityLog>();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT timestamp, username, action, detail, changed_by 
                    FROM user_activity_log 
                    ORDER BY timestamp DESC", conn);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new UserActivityLog
                    {
                        Timestamp = reader.GetDateTime(0),
                        Username = reader.GetString(1),
                        Action = reader.GetString(2),
                        Detail = reader.GetString(3),
                        ChangedBy = reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to retrieve logs: {ex.Message}");
                throw;
            }

            return logs;
        }

        public async Task<List<UserActivityLog>> GetLogsForUserAsync(string loginName)
        {
            var logs = new List<UserActivityLog>();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(@"
                    SELECT timestamp, username, action, detail, changed_by 
                    FROM user_activity_log 
                    WHERE LOWER(changed_by) = LOWER(@login) 
                    ORDER BY timestamp DESC", conn);

                cmd.Parameters.AddWithValue("@login", loginName);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    logs.Add(new UserActivityLog
                    {
                        Timestamp = reader.GetDateTime(0),
                        Username = reader.GetString(1),
                        Action = reader.GetString(2),
                        Detail = reader.GetString(3),
                        ChangedBy = reader.GetString(4)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to retrieve logs for user: {ex.Message}");
                throw;
            }

            return logs;
        }
    }
}
