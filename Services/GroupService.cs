using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

namespace CivilProcessERP.Services
{
    public class GroupService
    {
        private readonly string _connStr;

        public GroupService()
        {
            _connStr = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";
        }

        public async Task<List<string>> GetGroupsForUserAsync(int userId)
        {
            var groupNames = new List<string>();

            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT header.name
                FROM usergroupmember member
                JOIN usergroupheader header ON member.groupid = header.id
                WHERE member.usernumber = @uid", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                groupNames.Add(reader.GetString(0));
            }

            return groupNames;
        }

        public async Task SaveGroupsForUserAsync(int userId, List<string> groupNames)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // 1. Delete previous mappings
                var deleteCmd = new NpgsqlCommand("DELETE FROM usergroupmember WHERE usernumber = @uid", conn, tx);
                deleteCmd.Parameters.AddWithValue("@uid", userId);
                await deleteCmd.ExecuteNonQueryAsync();

                // 2. Insert new group mappings
                foreach (var name in groupNames)
                {
                    var groupIdCmd = new NpgsqlCommand("SELECT id FROM usergroupheader WHERE name = @name", conn, tx);
                    groupIdCmd.Parameters.AddWithValue("@name", name);
                    var groupIdObj = await groupIdCmd.ExecuteScalarAsync();

                    if (groupIdObj != null && groupIdObj is Guid groupId)
                    {
                        var insertCmd = new NpgsqlCommand(@"
                            INSERT INTO usergroupmember (id, usernumber, groupid, ts)
                            VALUES (gen_random_uuid(), @uid, @gid, current_timestamp)", conn, tx);

                        insertCmd.Parameters.AddWithValue("@uid", userId);
                        insertCmd.Parameters.AddWithValue("@gid", groupId);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception("Error while saving group assignments.", ex);
            }
        }

        public async Task<bool> CreateGroupAsync(string name)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM usergroupheader WHERE name = @n", conn);
            checkCmd.Parameters.AddWithValue("@n", name);
            var result = await checkCmd.ExecuteScalarAsync();
            var exists = (result != null && result != DBNull.Value) ? Convert.ToInt64(result) : 0;

            if (exists > 0) return false;

            var insertCmd = new NpgsqlCommand(
                "INSERT INTO usergroupheader (id, name, active, ts) VALUES (gen_random_uuid(), @n, true, current_timestamp)", conn);
            insertCmd.Parameters.AddWithValue("@n", name);
            await insertCmd.ExecuteNonQueryAsync();

            return true;
        }

        public async Task<List<string>> GetAllGroupNamesAsync()
        {
            var list = new List<string>();
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand("SELECT name FROM usergroupheader WHERE active = true", conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                list.Add(reader.GetString(0));

            return list;
        }

        public async Task<List<UserModel>> GetUsersForGroupAsync(string groupName)
        {
            var users = new List<UserModel>();
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            var cmd = new NpgsqlCommand(@"
                SELECT u.usernumber, u.loginname
                FROM usergroupmember m
                JOIN usergroupheader h ON m.groupid = h.id
                JOIN users u ON m.usernumber = u.usernumber
                WHERE h.name = @name", conn);

            cmd.Parameters.AddWithValue("@name", groupName);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new UserModel
                {
                    UserNumber = reader.GetInt32(0),
                    LoginName = reader.GetString(1)
                });
            }

            return users;
        }

        public async Task<bool> DeleteGroupAsync(string groupName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connStr);
                await conn.OpenAsync();

                Guid groupId;
                await using (var getIdCmd = new NpgsqlCommand("SELECT id FROM usergroupheader WHERE name = @name", conn))
                {
                    getIdCmd.Parameters.AddWithValue("name", groupName);
                    var result = await getIdCmd.ExecuteScalarAsync();
                    if (result == null) return false;
                    groupId = (Guid)result;
                }

                var cleanup = new NpgsqlCommand("DELETE FROM usergroupmember WHERE groupid = @groupid", conn);
                cleanup.Parameters.AddWithValue("groupid", groupId);
                await cleanup.ExecuteNonQueryAsync();

                var cmd = new NpgsqlCommand("DELETE FROM usergroupheader WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", groupId);
                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete group: {ex.Message}");
                return false;
            }
        }
    }
}