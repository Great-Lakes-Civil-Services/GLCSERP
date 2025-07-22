using System.Data;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

public class LoginService
{
    private readonly string _connectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866;Timeout=5;CommandTimeout=5";

    public async Task<UserModel?> AuthenticateUserAsync(string loginname, string password)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        Console.WriteLine("Opening DB connection...");
        await conn.OpenAsync();
        Console.WriteLine("DB connection opened.");

        var cmd = new NpgsqlCommand("SELECT * FROM users WHERE loginname = @login AND password = @pass", conn);
        cmd.Parameters.AddWithValue("@login", loginname);
        cmd.Parameters.AddWithValue("@pass", password); // ⚠️ Ideally hashed

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
                Enabled = reader.GetBoolean(reader.GetOrdinal("enabled")),
                MfaEnabled = reader.GetBoolean(reader.GetOrdinal("mfa_enabled")),
                MfaSecret = reader.IsDBNull(reader.GetOrdinal("mfa_secret")) ? string.Empty : reader.GetString(reader.GetOrdinal("mfa_secret")),
                EntityNumber = reader.GetInt32(reader.GetOrdinal("entitynumber")),
                MfaLastVerifiedAt = reader.IsDBNull(reader.GetOrdinal("mfa_last_verified_at")) 
                    ? (DateTime?)null 
                    : reader.GetDateTime(reader.GetOrdinal("mfa_last_verified_at"))
            };
        }

        return null;
    }

    public async Task UpdateUserMfaAsync(UserModel user)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"UPDATE users
                    SET mfa_enabled = @enabled,
                        mfa_secret = @secret
                    WHERE loginname = @username";

        var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@enabled", user.MfaEnabled);
        cmd.Parameters.AddWithValue("@secret", user.MfaSecret ?? "");
        cmd.Parameters.AddWithValue("@username", user.LoginName);

        await cmd.ExecuteNonQueryAsync();
    }
}
