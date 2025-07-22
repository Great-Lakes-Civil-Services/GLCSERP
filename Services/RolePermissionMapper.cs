using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

namespace CivilProcessERP.Services
{
    public static class RolePermissionMapper
    {
        private const string ConnectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

        public static async Task<List<PermissionModel>> GetPermissionsForRoleAsync(int roleNumber)
        {
            var permissions = new List<PermissionModel>();

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                string query = @"
                    SELECT p.name
                    FROM rolepermissions rp
                    JOIN permissions p ON rp.permissionid = p.id
                    WHERE rp.rolenumber = @roleNumber
                ";

                await using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("roleNumber", roleNumber);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    permissions.Add(new PermissionModel
                    {
                        Permission = reader.GetString(0),
                        IsGranted = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load permissions for role {roleNumber}: {ex.Message}");
            }

            return permissions;
        }
    }
}
