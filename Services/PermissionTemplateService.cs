using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

public class PermissionTemplateService
{
    private readonly string _connStr = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public async Task<List<string>> GetAllTemplateNamesAsync()
    {
        var list = new List<string>();

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT name FROM permission_templates WHERE active = TRUE", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }

    public async Task<List<string>> GetPermissionsForTemplateAsync(string templateName)
    {
        var list = new List<string>();

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(@"
            SELECT p.name
            FROM permission_templates t
            JOIN permission_template_mapping m ON t.id = m.template_id
            JOIN permissions p ON m.permission_id = p.id
            WHERE t.name = @template", conn);

        cmd.Parameters.AddWithValue("@template", templateName);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }
}
