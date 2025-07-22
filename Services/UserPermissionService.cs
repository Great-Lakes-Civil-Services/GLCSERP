using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

namespace CivilProcessERP.Services
{
    public class UserPermissionService
    {
        private readonly string _connStr = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

        // 🔹 Save direct permissions asynchronously
        public async Task SavePermissionsForUserAsync(int userId, List<string> permissionNames)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Delete old direct permissions
                var del = new NpgsqlCommand("DELETE FROM userpermissions WHERE usernumber = @uid", conn);
                del.Parameters.AddWithValue("@uid", userId);
                await del.ExecuteNonQueryAsync();

                // Insert new ones
                foreach (var name in permissionNames)
                {
                    var permIdCmd = new NpgsqlCommand("SELECT id FROM permissions WHERE name = @name", conn);
                    permIdCmd.Parameters.AddWithValue("@name", name);
                    var permId = await permIdCmd.ExecuteScalarAsync() as int?;

                    if (permId.HasValue)
                    {
                        var insert = new NpgsqlCommand(@"
                            INSERT INTO userpermissions (usernumber, permissionid)
                            VALUES (@uid, @pid)", conn);

                        insert.Parameters.AddWithValue("@uid", userId);
                        insert.Parameters.AddWithValue("@pid", permId.Value);
                        await insert.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception("Error saving user permissions.", ex);
            }
        }

        // 🔹 Fetch direct permissions asynchronously
        public async Task<List<string>> GetPermissionsForUserAsync(int userId)
        {
            var list = new List<string>();
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT p.name
                FROM userpermissions up
                JOIN permissions p ON p.id = up.permissionid
                WHERE up.usernumber = @uid", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));

            return list;
        }
    }
}
