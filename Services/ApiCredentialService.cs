using System;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

public class ApiCredentialService
{
    private readonly string _connString;

    public ApiCredentialService(string connectionString)
    {
        _connString = connectionString;
    }

    public async Task CreateOrUpdateCredentialAsync(int userNumber, string accessKey, string secretKey)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            var sql = @"
                INSERT INTO user_api_credentials (usernumber, access_key, secret_key)
                VALUES (@user, @access, @secret)
                ON CONFLICT (usernumber) DO UPDATE
                SET access_key = @access, secret_key = @secret, created_at = NOW();
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user", userNumber);
            cmd.Parameters.AddWithValue("@access", accessKey);
            cmd.Parameters.AddWithValue("@secret", secretKey);

            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to create/update API credential: {ex.Message}");
            throw;
        }
    }

    public async Task<ApiCredentialModel?> GetCredentialAsync(int userNumber)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_connString);
            await conn.OpenAsync();

            var sql = "SELECT * FROM user_api_credentials WHERE usernumber = @user";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user", userNumber);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ApiCredentialModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    UserNumber = reader.GetInt32(reader.GetOrdinal("usernumber")),
                    AccessKey = reader.GetString(reader.GetOrdinal("access_key")),
                    SecretKey = reader.GetString(reader.GetOrdinal("secret_key")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    LastUsedAt = reader.IsDBNull(reader.GetOrdinal("last_used_at"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("last_used_at"))
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to retrieve API credential: {ex.Message}");
            throw;
        }
    }
}
