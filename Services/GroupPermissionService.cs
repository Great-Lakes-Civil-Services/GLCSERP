using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Services
{
    public class GroupPermissionService
    {
        private readonly string _connStr;

        public GroupPermissionService(string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=7866;Database=mypg_database")
        {
            _connStr = connectionString;
        }

        // 🔹 Get list of all available permissions
        public async Task<List<string>> GetAllPermissionsAsync()
        {
            var list = new List<string>();
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT name FROM permissions ORDER BY name", conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(reader.GetString(0));
            }

            return list;
        }

        // 🔹 Get all permissions assigned to a given group
        public async Task<List<string>> GetPermissionsForGroupAsync(string groupName)
        {
            var result = new List<string>();
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT p.name
                FROM grouppermissions gp
                JOIN permissions p ON gp.permissionid = p.id
                JOIN usergroupheader h ON h.id = gp.groupid
                WHERE h.name = @name", conn);

            cmd.Parameters.AddWithValue("@name", groupName);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(reader.GetString(0));
            }

            return result;
        }

        // 🔹 Save/overwrite permissions for a given group
        public async Task SavePermissionsForGroupAsync(string groupName, List<string> permissions)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                var groupIdCmd = new NpgsqlCommand("SELECT id FROM usergroupheader WHERE name = @n", conn, tx);
                groupIdCmd.Parameters.AddWithValue("@n", groupName);
                var groupIdObj = await groupIdCmd.ExecuteScalarAsync();

                if (groupIdObj is not Guid groupId)
                    throw new Exception("Group not found.");

                var deleteCmd = new NpgsqlCommand("DELETE FROM grouppermissions WHERE groupid = @gid", conn, tx);
                deleteCmd.Parameters.AddWithValue("@gid", groupId);
                await deleteCmd.ExecuteNonQueryAsync();

                foreach (var perm in permissions)
                {
                    var pidCmd = new NpgsqlCommand("SELECT id FROM permissions WHERE name = @name", conn, tx);
                    pidCmd.Parameters.AddWithValue("@name", perm);
                    var pidObj = await pidCmd.ExecuteScalarAsync();

                    if (pidObj is int pid)
                    {
                        var insertCmd = new NpgsqlCommand(@"
                            INSERT INTO grouppermissions (groupid, permissionid)
                            VALUES (@gid, @pid)", conn, tx);

                        insertCmd.Parameters.AddWithValue("@gid", groupId);
                        insertCmd.Parameters.AddWithValue("@pid", pid);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception("Error saving group permissions.", ex);
            }
        }

        // 🔹 Add a new permission if it doesn't exist
        public async Task<bool> AddPermissionAsync(string permissionName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand(
                    "INSERT INTO permissions (name) VALUES (@name) ON CONFLICT (name) DO NOTHING", conn);

                cmd.Parameters.AddWithValue("name", permissionName);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add permission: {ex.Message}");
                return false;
            }
        }

        // 🔹 Delete a permission by name
        public async Task<bool> DeletePermissionAsync(string permissionName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connStr);
                await conn.OpenAsync();

                var cmd = new NpgsqlCommand("DELETE FROM permissions WHERE name = @name", conn);
                cmd.Parameters.AddWithValue("name", permissionName);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete permission: {ex.Message}");
                return false;
            }
        }
    }
}
