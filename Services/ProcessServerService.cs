using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;
using System.Collections.Generic;

namespace CivilProcessERP.Services
{
    public class ProcessServerService
    {
        private readonly string _connectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866;Timeout=5;CommandTimeout=5";

        /// <summary>
        /// Toggles the process server status for a user
        /// </summary>
        /// <param name="userNumber">The user number to toggle</param>
        /// <param name="newStatus">The new process server status</param>
        /// <param name="changedBy">The user making the change</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ToggleProcessServerStatusAsync(int userNumber, bool newStatus, string changedBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Update the process server status
                var updateSql = @"
                    UPDATE users 
                    SET process_server_status = @newStatus,
                        change_number = change_number + 1,
                        update_id = gen_random_uuid(),
                        timestamp = CURRENT_TIMESTAMP
                    WHERE usernumber = @userNumber";

                var updateCmd = new NpgsqlCommand(updateSql, conn);
                updateCmd.Parameters.AddWithValue("@newStatus", newStatus);
                updateCmd.Parameters.AddWithValue("@userNumber", userNumber);

                var rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    // Log the change
                    await LogProcessServerStatusChangeAsync(userNumber, newStatus, changedBy);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to toggle process server status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the current process server status for a user
        /// </summary>
        /// <param name="userNumber">The user number</param>
        /// <returns>The current process server status</returns>
        public async Task<bool> GetProcessServerStatusAsync(int userNumber)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = "SELECT process_server_status FROM users WHERE usernumber = @userNumber";
                var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userNumber", userNumber);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get process server status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all users with their process server status
        /// </summary>
        /// <returns>List of users with process server status</returns>
        public async Task<List<UserModel>> GetAllUsersWithProcessServerStatusAsync()
        {
            var users = new List<UserModel>();

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT usernumber, loginname, firstname, lastname, 
                           enabled, process_server_status
                    FROM users 
                    ORDER BY loginname";

                var cmd = new NpgsqlCommand(sql, conn);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new UserModel
                    {
                        UserNumber = reader.GetInt32("usernumber"),
                        LoginName = reader.GetString("loginname"),
                        FirstName = reader.GetString("firstname"),
                        LastName = reader.GetString("lastname"),
                        Enabled = reader.GetBoolean("enabled"),
                        ProcessServerStatus = reader.GetBoolean("process_server_status")
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get users with process server status: {ex.Message}");
            }

            return users;
        }

        /// <summary>
        /// Logs the process server status change for audit purposes
        /// </summary>
        private async Task LogProcessServerStatusChangeAsync(int userNumber, bool newStatus, string changedBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO user_activity_log (user_number, action, detail, changed_by, timestamp)
                    VALUES (@userNumber, @action, @detail, @changedBy, CURRENT_TIMESTAMP)";

                var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@userNumber", userNumber);
                cmd.Parameters.AddWithValue("@action", "PROCESS_SERVER_STATUS_CHANGE");
                cmd.Parameters.AddWithValue("@detail", $"Process Server Status changed to: {(newStatus ? "Enabled" : "Disabled")}");
                cmd.Parameters.AddWithValue("@changedBy", changedBy);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to log process server status change: {ex.Message}");
            }
        }
    }
} 