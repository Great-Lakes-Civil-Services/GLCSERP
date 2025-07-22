using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

namespace CivilProcessERP.Services
{
    public class RolePermissionService
    {
        private readonly string _connStr = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

        public async Task<List<PermissionModel>> GetPermissionsForRoleAsync(int roleNumber)
        {
            var permissions = new List<PermissionModel>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            string query = @"
                SELECT p.name
                FROM rolepermissions rp
                JOIN permissions p ON rp.permissionid = p.id
                WHERE rp.roleid = @roleId;
            ";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@roleId", roleNumber);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permissions.Add(new PermissionModel
                {
                    Permission = reader.GetString(0),
                    IsGranted = true
                });
            }

            return permissions;
        }

        public async Task<List<string>> GetAllPermissionsAsync()
        {
            var list = new List<string>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT name FROM permissions ORDER BY name", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));

            return list;
        }

       public async Task<string?> GetRoleNameByIdAsync(int roleNumber)
{
    await using var conn = new NpgsqlConnection(_connStr);
    await conn.OpenAsync();

    string query = "SELECT rolename FROM roles WHERE rolenumber = @roleNumber";
    await using var cmd = new NpgsqlCommand(query, conn);

    // ✅ Fix: use "@roleNumber" instead of "roleNumber"
    cmd.Parameters.AddWithValue("@roleNumber", roleNumber);

    var result = await cmd.ExecuteScalarAsync();
    return result != null && result != DBNull.Value ? result.ToString() : null;
}


        public async Task<Dictionary<string, int>> GetAllRolesAsync()
        {
            var result = new Dictionary<string, int>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT rolename, rolenumber FROM roles ORDER BY rolename", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string roleName = reader.GetString(0);
                int roleNumber = reader.GetInt32(1);
                result[roleName] = roleNumber;
            }

            return result;
        }

        public async Task SavePermissionsForRoleAsync(int roleId, List<string> permissionNames)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            // 1. Delete existing mappings
            var deleteCmd = new NpgsqlCommand("DELETE FROM rolepermissions WHERE roleid = @roleId", conn);
            deleteCmd.Parameters.AddWithValue("@roleId", roleId);
            await deleteCmd.ExecuteNonQueryAsync();

            // 2. Insert new mappings
            foreach (string permission in permissionNames)
            {
                var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO rolepermissions (roleid, permissionid)
                    SELECT @roleId, id FROM permissions WHERE name = @permName
                ", conn);

                insertCmd.Parameters.AddWithValue("@roleId", roleId);
                insertCmd.Parameters.AddWithValue("@permName", permission);
                await insertCmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
    }
}
