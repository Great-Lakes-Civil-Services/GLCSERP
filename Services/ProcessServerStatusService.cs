using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models;

namespace CivilProcessERP.Services
{
    public class ProcessServerStatusService
    {
        private readonly string _connectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866;Timeout=5;CommandTimeout=5";

        /// <summary>
        /// Gets all distinct process servers directly from entity table
        /// </summary>
        public async Task<List<string>> GetAllDistinctProcessServersAsync()
        {
            var processServers = new List<string>();
            
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Get process server names directly from entity table
                var sql = @"
                    SELECT DISTINCT ""FirstName"", ""LastName"", ""SerialNum""
                    FROM entity 
                    WHERE (""FirstName"" IS NOT NULL AND ""FirstName"" != '') 
                    OR (""LastName"" IS NOT NULL AND ""LastName"" != '')
                    ORDER BY ""FirstName"", ""LastName""";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var firstName = reader["FirstName"]?.ToString() ?? "";
                    var lastName = reader["LastName"]?.ToString() ?? "";
                    
                    string fullName;
                    if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                    {
                        fullName = $"{firstName} {lastName}".Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(lastName))
                    {
                        fullName = lastName.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(firstName))
                    {
                        fullName = firstName.Trim();
                    }
                    else
                    {
                        continue; // Skip if both are empty
                    }
                    
                    if (!string.IsNullOrWhiteSpace(fullName) && !processServers.Contains(fullName))
                    {
                        processServers.Add(fullName);
                    }
                }

                Console.WriteLine($"[INFO] Found {processServers.Count} distinct process servers");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get distinct process servers: {ex.Message}");
            }

            return processServers;
        }

        /// <summary>
        /// Gets process server status for a specific job
        /// </summary>
        public async Task<bool> GetProcessServerStatusForJobAsync(string jobId, string processServerName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT is_active 
                    FROM process_server_status 
                    WHERE job_id = @jobId AND process_server_name = @processServerName";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@jobId", jobId);
                cmd.Parameters.AddWithValue("@processServerName", processServerName);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && (bool)result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get process server status for job {jobId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Toggles process server status for a specific job
        /// </summary>
        public async Task<bool> ToggleProcessServerStatusForJobAsync(string jobId, string processServerName, bool newStatus, string changedBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Check if record exists
                var checkSql = @"
                    SELECT id FROM process_server_status 
                    WHERE job_id = @jobId AND process_server_name = @processServerName";

                await using var checkCmd = new NpgsqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@jobId", jobId);
                checkCmd.Parameters.AddWithValue("@processServerName", processServerName);

                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Update existing record
                    var updateSql = @"
                        UPDATE process_server_status 
                        SET is_active = @isActive, updated_by = @updatedBy, updated_at = CURRENT_TIMESTAMP
                        WHERE job_id = @jobId AND process_server_name = @processServerName";

                    await using var updateCmd = new NpgsqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@isActive", newStatus);
                    updateCmd.Parameters.AddWithValue("@updatedBy", changedBy);
                    updateCmd.Parameters.AddWithValue("@jobId", jobId);
                    updateCmd.Parameters.AddWithValue("@processServerName", processServerName);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new record
                    var insertSql = @"
                        INSERT INTO process_server_status (job_id, process_server_name, is_active, created_by, updated_by)
                        VALUES (@jobId, @processServerName, @isActive, @createdBy, @updatedBy)";

                    await using var insertCmd = new NpgsqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@jobId", jobId);
                    insertCmd.Parameters.AddWithValue("@processServerName", processServerName);
                    insertCmd.Parameters.AddWithValue("@isActive", newStatus);
                    insertCmd.Parameters.AddWithValue("@createdBy", changedBy);
                    insertCmd.Parameters.AddWithValue("@updatedBy", changedBy);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                // Log the change
                await LogProcessServerStatusChangeAsync(jobId, processServerName, newStatus, changedBy);

                Console.WriteLine($"[SUCCESS] Toggled process server status for job {jobId}, process server {processServerName} to {newStatus}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to toggle process server status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all jobs for a specific process server
        /// </summary>
        public async Task<List<ProcessServerJobInfo>> GetJobsForProcessServerAsync(string processServerName)
        {
            var jobs = new List<ProcessServerJobInfo>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Handle different name formats
                string firstName, lastName;
                
                if (processServerName.Contains(' '))
                {
                    // Standard "FirstName LastName" format
                    var nameParts = processServerName.Split(' ', 2);
                    if (nameParts.Length != 2)
                    {
                        Console.WriteLine($"[ERROR] Invalid process server name format: {processServerName}");
                        return jobs;
                    }
                    firstName = nameParts[0];
                    lastName = nameParts[1];
                }
                else
                {
                    // Single name format (like "030707lt") - treat as last name
                    firstName = "";
                    lastName = processServerName;
                }

                // First, find the SerialNum for this process server name
                string serialNumSql;
                if (string.IsNullOrEmpty(firstName))
                {
                    serialNumSql = @"
                        SELECT ""SerialNum"" 
                        FROM entity 
                        WHERE ""LastName"" = @processServerName";
                }
                else
                {
                    serialNumSql = @"
                        SELECT ""SerialNum"" 
                        FROM entity 
                        WHERE (""FirstName"" = @firstName AND ""LastName"" = @lastName)
                        OR (""FirstName"" || ' ' || ""LastName"" = @processServerName)
                        OR (""LastName"" = @processServerName)";
                }

                await using var cmd1 = new NpgsqlCommand(serialNumSql, conn);
                if (!string.IsNullOrEmpty(firstName))
                {
                    cmd1.Parameters.AddWithValue("@firstName", firstName);
                    cmd1.Parameters.AddWithValue("@lastName", lastName);
                    cmd1.Parameters.AddWithValue("@processServerName", processServerName);
                }
                else
                {
                    cmd1.Parameters.AddWithValue("@processServerName", processServerName);
                }

                var serialNum = await cmd1.ExecuteScalarAsync();
                if (serialNum == null)
                {
                    Console.WriteLine($"[WARN] No SerialNum found for process server: {processServerName}");
                    return jobs;
                }

                Console.WriteLine($"[DEBUG] Found SerialNum {serialNum} for process server: {processServerName}");

                // Now get jobs that use this SerialNum as servercode in papers table
                // Link: entity.SerialNum -> papers.servercode -> papers.serialnum (this is the job_id)
                var sql = @"
                    SELECT DISTINCT p.serialnum as job_id, p.caseserialnum as case_number, 
                           e1.""LastName"" as defendant, e2.""LastName"" as plaintiff,
                           COALESCE(pss.is_active, true) as is_active
                    FROM papers p
                    LEFT JOIN entity e1 ON p.clientnum = e1.""SerialNum""
                    LEFT JOIN entity e2 ON p.pliannum = e2.""SerialNum""
                    LEFT JOIN process_server_status pss ON p.serialnum::varchar = pss.job_id AND @processServerName = pss.process_server_name
                    WHERE p.servercode::text = @serialNum::text
                    ORDER BY p.serialnum";

                await using var cmd2 = new NpgsqlCommand(sql, conn);
                cmd2.Parameters.AddWithValue("@serialNum", serialNum);
                cmd2.Parameters.AddWithValue("@processServerName", processServerName);

                Console.WriteLine($"[DEBUG] Executing SQL with serialNum={serialNum}, processServerName={processServerName}");

                await using var reader = await cmd2.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    jobs.Add(new ProcessServerJobInfo
                    {
                        JobId = reader["job_id"]?.ToString() ?? "",
                        CaseNumber = reader["case_number"]?.ToString() ?? "",
                        Defendant = reader["defendant"]?.ToString() ?? "",
                        Plaintiff = reader["plaintiff"]?.ToString() ?? "",
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
                    });
                }
                Console.WriteLine($"[INFO] Found {jobs.Count} jobs for process server: {processServerName} (SerialNum: {serialNum})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to load jobs for process server {processServerName}: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
            return jobs;
        }

        /// <summary>
        /// Logs process server status changes to the audit log
        /// </summary>
        private async Task LogProcessServerStatusChangeAsync(string jobId, string processServerName, bool newStatus, string changedBy)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var sql = @"
                    INSERT INTO user_activity_log (timestamp, username, action, detail, changed_by)
                    VALUES (@timestamp, @username, @action, @detail, @changedBy)";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@username", "System");
                cmd.Parameters.AddWithValue("@action", "ProcessServerStatusChange");
                cmd.Parameters.AddWithValue("@detail", $"Job {jobId}: Process server '{processServerName}' status changed to {(newStatus ? "Active" : "Inactive")}");
                cmd.Parameters.AddWithValue("@changedBy", changedBy);

                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"[INFO] Logged process server status change for job {jobId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to log process server status change: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a new process server to the entity table
        /// </summary>
        public async Task<bool> AddProcessServerAsync(string firstName, string lastName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Check if process server already exists
                string checkSql;
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    checkSql = @"
                        SELECT COUNT(*) 
                        FROM entity 
                        WHERE ""LastName"" = @lastName";
                }
                else
                {
                    checkSql = @"
                        SELECT COUNT(*) 
                        FROM entity 
                        WHERE ""FirstName"" = @firstName AND ""LastName"" = @lastName";
                }

                await using var checkCmd = new NpgsqlCommand(checkSql, conn);
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    checkCmd.Parameters.AddWithValue("@firstName", firstName);
                }
                checkCmd.Parameters.AddWithValue("@lastName", lastName);

                var existingCount = await checkCmd.ExecuteScalarAsync();
                if (existingCount != null && Convert.ToInt32(existingCount) > 0)
                {
                    Console.WriteLine($"[WARN] Process server '{firstName} {lastName}' already exists");
                    return false;
                }

                // Get next SerialNum
                var maxSerialSql = "SELECT COALESCE(MAX(\"SerialNum\"), 0) + 1 FROM entity";
                await using var maxSerialCmd = new NpgsqlCommand(maxSerialSql, conn);
                var nextSerialNum = await maxSerialCmd.ExecuteScalarAsync();

                // Insert new process server - handle empty firstName
                string insertSql;
                if (string.IsNullOrWhiteSpace(firstName))
                {
                    insertSql = @"
                        INSERT INTO entity (""SerialNum"", ""LastName"", ""ChangeNum"")
                        VALUES (@serialNum, @lastName, @changeNum)";
                }
                else
                {
                    insertSql = @"
                        INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"", ""ChangeNum"")
                        VALUES (@serialNum, @firstName, @lastName, @changeNum)";
                }

                await using var insertCmd = new NpgsqlCommand(insertSql, conn);
                insertCmd.Parameters.AddWithValue("@serialNum", nextSerialNum);
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    insertCmd.Parameters.AddWithValue("@firstName", firstName);
                }
                insertCmd.Parameters.AddWithValue("@lastName", lastName);
                insertCmd.Parameters.AddWithValue("@changeNum", 0);

                await insertCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"[SUCCESS] Added new process server: {firstName} {lastName} with SerialNum: {nextSerialNum}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to add process server: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a process server from the entity table
        /// </summary>
        public async Task<bool> DeleteProcessServerAsync(string processServerName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // Handle different name formats
                string firstName, lastName;
                
                if (processServerName.Contains(' '))
                {
                    // Standard "FirstName LastName" format
                    var nameParts = processServerName.Split(' ', 2);
                    if (nameParts.Length != 2)
                    {
                        Console.WriteLine($"[ERROR] Invalid process server name format: {processServerName}");
                        return false;
                    }
                    firstName = nameParts[0];
                    lastName = nameParts[1];
                }
                else
                {
                    // Single name format (like "030707lt") - treat as last name
                    firstName = "";
                    lastName = processServerName;
                }

                // Check if process server exists
                string checkSql;
                if (string.IsNullOrEmpty(firstName))
                {
                    checkSql = @"
                        SELECT COUNT(*) 
                        FROM entity 
                        WHERE ""LastName"" = @lastName";
                }
                else
                {
                    checkSql = @"
                        SELECT COUNT(*) 
                        FROM entity 
                        WHERE ""FirstName"" = @firstName AND ""LastName"" = @lastName";
                }

                await using var checkCmd = new NpgsqlCommand(checkSql, conn);
                if (!string.IsNullOrEmpty(firstName))
                {
                    checkCmd.Parameters.AddWithValue("@firstName", firstName);
                }
                checkCmd.Parameters.AddWithValue("@lastName", lastName);

                var existingCount = await checkCmd.ExecuteScalarAsync();
                if (existingCount == null || Convert.ToInt32(existingCount) == 0)
                {
                    Console.WriteLine($"[WARN] Process server '{processServerName}' not found");
                    return false;
                }

                // Delete the process server
                string deleteSql;
                if (string.IsNullOrEmpty(firstName))
                {
                    deleteSql = @"
                        DELETE FROM entity 
                        WHERE ""LastName"" = @lastName";
                }
                else
                {
                    deleteSql = @"
                        DELETE FROM entity 
                        WHERE ""FirstName"" = @firstName AND ""LastName"" = @lastName";
                }

                await using var deleteCmd = new NpgsqlCommand(deleteSql, conn);
                if (!string.IsNullOrEmpty(firstName))
                {
                    deleteCmd.Parameters.AddWithValue("@firstName", firstName);
                }
                deleteCmd.Parameters.AddWithValue("@lastName", lastName);

                await deleteCmd.ExecuteNonQueryAsync();

                Console.WriteLine($"[SUCCESS] Deleted process server: {processServerName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to delete process server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests the database connection and entity table structure
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                Console.WriteLine("[INFO] Database connection successful");

                // Test if entity table exists and get its structure
                var tableExistsSql = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = 'entity'
                    )";

                await using var tableExistsCmd = new NpgsqlCommand(tableExistsSql, conn);
                var tableExists = await tableExistsCmd.ExecuteScalarAsync();
                
                if (tableExists != null && (bool)tableExists)
                {
                    Console.WriteLine("[INFO] Entity table exists");
                    
                    // Get column information
                    var columnsSql = @"
                        SELECT column_name, data_type, is_nullable
                        FROM information_schema.columns 
                        WHERE table_name = 'entity'
                        ORDER BY ordinal_position";

                    await using var columnsCmd = new NpgsqlCommand(columnsSql, conn);
                    await using var columnsReader = await columnsCmd.ExecuteReaderAsync();
                    
                    Console.WriteLine("[INFO] Entity table columns:");
                    while (await columnsReader.ReadAsync())
                    {
                        var columnName = columnsReader["column_name"]?.ToString();
                        var dataType = columnsReader["data_type"]?.ToString();
                        var isNullable = columnsReader["is_nullable"]?.ToString();
                        Console.WriteLine($"  - {columnName}: {dataType} (nullable: {isNullable})");
                    }
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("[ERROR] Entity table does not exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Debug method to test data linking step by step
        /// </summary>
        public async Task DebugProcessServerJobsAsync(string processServerName)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                Console.WriteLine($"[DEBUG] === Testing data linking for: {processServerName} ===");

                // Step 1: Find SerialNum in entity table
                var entitySql = @"
                    SELECT ""SerialNum"", ""FirstName"", ""LastName""
                    FROM entity 
                    WHERE ""FirstName"" || ' ' || ""LastName"" = @processServerName
                    OR ""LastName"" = @processServerName";

                await using var entityCmd = new NpgsqlCommand(entitySql, conn);
                entityCmd.Parameters.AddWithValue("@processServerName", processServerName);

                await using var entityReader = await entityCmd.ExecuteReaderAsync();
                if (await entityReader.ReadAsync())
                {
                    var serialNum = entityReader["SerialNum"]?.ToString();
                    var firstName = entityReader["FirstName"]?.ToString();
                    var lastName = entityReader["LastName"]?.ToString();
                    
                    Console.WriteLine($"[DEBUG] Step 1 - Entity found: SerialNum={serialNum}, FirstName='{firstName}', LastName='{lastName}'");
                    
                    if (!string.IsNullOrEmpty(serialNum))
                    {
                        // Step 2: Find papers with this servercode
                        await entityReader.CloseAsync();
                        
                        var papersSql = @"
                            SELECT serialnum, servercode
                            FROM papers 
                            WHERE servercode::text = @serialNum
                            LIMIT 10";

                        await using var papersCmd = new NpgsqlCommand(papersSql, conn);
                        papersCmd.Parameters.AddWithValue("@serialNum", serialNum);

                        await using var papersReader = await papersCmd.ExecuteReaderAsync();
                        var paperCount = 0;
                        while (await papersReader.ReadAsync())
                        {
                            var paperSerialNum = papersReader["serialnum"]?.ToString();
                            var paperServerCode = papersReader["servercode"]?.ToString();
                            
                            Console.WriteLine($"[DEBUG] Step 2 - Paper found: serialnum={paperSerialNum}, servercode={paperServerCode}");
                            paperCount++;
                        }
                        
                        Console.WriteLine($"[DEBUG] Step 2 - Total papers found: {paperCount}");

                        // Step 3: Test papers data directly
                        await papersReader.CloseAsync();
                        await TestPapersDataAsync(serialNum);

                        // Step 4: Find jobs linked to these papers
                        if (paperCount > 0)
                        {
                            var jobsSql = @"
                                SELECT p.serialnum as job_id, p.caseserialnum as case_number, 
                                       e1.""LastName"" as defendant, e2.""LastName"" as plaintiff
                                FROM papers p
                                LEFT JOIN entity e1 ON p.clientnum = e1.""SerialNum""
                                LEFT JOIN entity e2 ON p.pliannum = e2.""SerialNum""
                                WHERE p.servercode::text = @serialNum
                                LIMIT 10";

                            await using var jobsCmd = new NpgsqlCommand(jobsSql, conn);
                            jobsCmd.Parameters.AddWithValue("@serialNum", serialNum);

                            await using var jobsReader = await jobsCmd.ExecuteReaderAsync();
                            var jobCount = 0;
                            while (await jobsReader.ReadAsync())
                            {
                                var jobId = jobsReader["job_id"]?.ToString();
                                var caseNumber = jobsReader["case_number"]?.ToString();
                                var defendant = jobsReader["defendant"]?.ToString();
                                var plaintiff = jobsReader["plaintiff"]?.ToString();
                                
                                Console.WriteLine($"[DEBUG] Step 4 - Job found: job_id={jobId}, case_number='{caseNumber}', defendant='{defendant}', plaintiff='{plaintiff}'");
                                jobCount++;
                            }
                            
                            Console.WriteLine($"[DEBUG] Step 4 - Total jobs found: {jobCount}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Step 1 - No entity found for: {processServerName}");
                }

                Console.WriteLine($"[DEBUG] === End debug for: {processServerName} ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Debug failed: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Simple test to check papers table data
        /// </summary>
        public async Task TestPapersDataAsync(string serverCode)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                Console.WriteLine($"[DEBUG] === Testing papers data for servercode: {serverCode} ===");

                var sql = @"
                    SELECT serialnum, servercode
                    FROM papers 
                    WHERE servercode::text = @serverCode
                    LIMIT 5";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@serverCode", serverCode);

                await using var reader = await cmd.ExecuteReaderAsync();
                var count = 0;
                while (await reader.ReadAsync())
                {
                    var serialnum = reader["serialnum"]?.ToString();
                    var servercode = reader["servercode"]?.ToString();
                    Console.WriteLine($"[DEBUG] Paper: serialnum={serialnum}, servercode={servercode}");
                    count++;
                }
                
                Console.WriteLine($"[DEBUG] Total papers found for servercode {serverCode}: {count}");
                Console.WriteLine($"[DEBUG] === End papers test ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Papers test failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Model for process server job information
    /// </summary>
    public class ProcessServerJobInfo
    {
        public string JobId { get; set; } = "";
        public string CaseNumber { get; set; } = "";
        public string Defendant { get; set; } = "";
        public string Plaintiff { get; set; } = "";
        public bool IsActive { get; set; }
    }
} 