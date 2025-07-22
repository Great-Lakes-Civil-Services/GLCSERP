using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

public class UserSearchService
{
    private readonly string _connString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public async Task<List<UserModel>> SearchUsersAsync(string prefix)
    {
        var users = new List<UserModel>();
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT usernumber, loginname, firstname, lastname, rolenumber, entitynumber, enabled, mfa_enabled, mfa_last_verified_at " +
            "FROM users WHERE loginname ILIKE @prefix || '%' ORDER BY loginname", conn);
        cmd.Parameters.AddWithValue("@prefix", prefix);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new UserModel
            {
                UserNumber = reader.GetInt32(reader.GetOrdinal("usernumber")),
                LoginName = reader.GetString(reader.GetOrdinal("loginname")),
                FirstName = reader.GetString(reader.GetOrdinal("firstname")),
                LastName = reader.GetString(reader.GetOrdinal("lastname")),
                RoleNumber = reader.GetInt32(reader.GetOrdinal("rolenumber")),
                EntityNumber = reader.GetInt32(reader.GetOrdinal("entitynumber")),
                Enabled = reader.GetBoolean(reader.GetOrdinal("enabled")),
                MfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_enabled")),
                MfaLastVerifiedAt = reader.IsDBNull(reader.GetOrdinal("mfa_last_verified_at"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("mfa_last_verified_at"))
            });
        }

        return users;
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        var list = new List<UserModel>();
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT usernumber, loginname, rolenumber, mfa_enabled, mfa_last_verified_at FROM users ORDER BY loginname", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new UserModel
            {
                UserNumber = reader.GetInt32(reader.GetOrdinal("usernumber")),
                LoginName = reader.GetString(reader.GetOrdinal("loginname")),
                RoleNumber = reader.GetInt32(reader.GetOrdinal("rolenumber")),
                MfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_enabled")),
                MfaLastVerifiedAt = reader.IsDBNull(reader.GetOrdinal("mfa_last_verified_at"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("mfa_last_verified_at"))
            });
        }

        return list;
    }

    public async Task<List<string>> GetAllLoginNamesAsync()
    {
        var names = new List<string>();
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("SELECT loginname FROM users ORDER BY loginname", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));

        return names;
    }

    public async Task UpdateMfaLastVerifiedAsync(string loginName, DateTime timestamp)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        int userNumber = -1;
        await using (var getUserCmd = new NpgsqlCommand("SELECT usernumber FROM users WHERE loginname = @login", conn))
        {
            getUserCmd.Parameters.AddWithValue("@login", loginName);
            var result = await getUserCmd.ExecuteScalarAsync();
            if (result != null && result is int)
            {
                userNumber = (int)result;
            }
            else
            {
                Console.WriteLine($"[WARN] User '{loginName}' not found.");
                return;
            }
        }

        await using var cmd = new NpgsqlCommand("UPDATE users SET mfa_last_verified_at = @ts WHERE usernumber = @id", conn);
        cmd.Parameters.AddWithValue("@ts", timestamp);
        cmd.Parameters.AddWithValue("@id", userNumber);
        int rows = await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"[INFO] Updated MFA timestamp for user '{loginName}' (UserNumber: {userNumber}) — Rows affected: {rows}");
    }

    public async Task<UserModel?> GetUserByLoginAsync(string loginName)
    {
        await using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();

        var query = @"
            SELECT usernumber, loginname, firstname, lastname, rolenumber, entitynumber,
                   enabled, passwordresetrequired, mfa_enabled, mfa_last_verified_at
            FROM users
            WHERE loginname = @loginname
            LIMIT 1";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@loginname", loginName);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new UserModel
            {
                UserNumber = reader.GetInt32(reader.GetOrdinal("usernumber")),
                LoginName = reader.GetString(reader.GetOrdinal("loginname")),
                FirstName = reader.GetString(reader.GetOrdinal("firstname")),
                LastName = reader.GetString(reader.GetOrdinal("lastname")),
                RoleNumber = reader.GetInt32(reader.GetOrdinal("rolenumber")),
                EntityNumber = reader.GetInt32(reader.GetOrdinal("entitynumber")),
                Enabled = reader.GetBoolean(reader.GetOrdinal("enabled")),
                MfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_enabled")),
                MfaLastVerifiedAt = reader.IsDBNull(reader.GetOrdinal("mfa_last_verified_at"))
                    ? (DateTime?)null
                    : reader.GetDateTime(reader.GetOrdinal("mfa_last_verified_at"))
            };
        }

        return null;
    }
}
