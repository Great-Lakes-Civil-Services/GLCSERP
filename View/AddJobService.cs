using System;
using System.Threading.Tasks;
using Npgsql;
using CivilProcessERP.Models.Job;
using System.Windows; // Added for MessageBox
using System.Collections.Generic; // Added for Dictionary
using System.Linq; // Added for Where, ToDictionary, Select
using System.Collections; // Added for HashSet

public class AddJobService
{
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public async Task AddJob(Job job)
    {
        // 1. Default SQLDATETIMECREATED to now if empty
        if (string.IsNullOrWhiteSpace(job.SqlDateTimeCreated))
            job.SqlDateTimeCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        if (string.IsNullOrWhiteSpace(job.CaseNumber))
            throw new ArgumentException("Case Number is required.");

        int caseSerial, paperSerial;
        // --- FIRST TRANSACTION: Insert into cases and papers ---
        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                caseSerial = await GenerateNewSerialAsync("cases", conn, tx);
                await using (var cmd = new NpgsqlCommand(@"INSERT INTO cases (serialnum, casenum) VALUES (@serialnum, @casenum);", conn, tx))
                {
                    cmd.Parameters.AddWithValue("serialnum", caseSerial);
                    cmd.Parameters.AddWithValue("casenum", job.CaseNumber);
                    await cmd.ExecuteNonQueryAsync();
                }

                paperSerial = await GenerateNewJobSerialAsync(conn, tx);
                // 5. Set datetimeserved if ServiceDate/ServiceTime provided
                object datetimeserved = DBNull.Value;
                if (!string.IsNullOrWhiteSpace(job.ServiceDate) && !string.IsNullOrWhiteSpace(job.ServiceTime))
                {
                    if (DateTime.TryParse($"{job.ServiceDate} {job.ServiceTime}", out var dtServed))
                        datetimeserved = dtServed;
                }
                Console.WriteLine($"[LOG] About to insert into papers: serialnum={paperSerial}, caseserialnum={caseSerial}, clientrefnum={job.ClientReference}, zone={job.Zone}, sqldatetimerecd={job.SqlDateTimeCreated}, sqldatetimeserved={job.LastDayToServe}, sqlexpiredate={job.ExpirationDate}, datetimeserved={datetimeserved}");
                await using (var cmdPapers = new NpgsqlCommand(@"INSERT INTO papers (serialnum, caseserialnum, clientrefnum, zone, sqldatetimerecd, sqldatetimeserved, sqlexpiredate, datetimeserved) VALUES (@serialnum, @caseserialnum, @clientrefnum, @zone, @sqlDateCreated, @lastDayToServe, @expirationDate, @datetimeserved);", conn, tx))
                {
                    cmdPapers.Parameters.AddWithValue("serialnum", paperSerial);
                    cmdPapers.Parameters.AddWithValue("caseserialnum", caseSerial);
                    cmdPapers.Parameters.AddWithValue("clientrefnum", job.ClientReference ?? (object)DBNull.Value);
                    cmdPapers.Parameters.AddWithValue("zone", job.Zone ?? (object)DBNull.Value);
                    cmdPapers.Parameters.AddWithValue("sqlDateCreated", ParseDateTimeOrNull(job.SqlDateTimeCreated));
                    cmdPapers.Parameters.AddWithValue("lastDayToServe", ParseDateTimeOrNull(job.LastDayToServe));
                    cmdPapers.Parameters.AddWithValue("expirationDate", ParseDateTimeOrNull(job.ExpirationDate));
                    cmdPapers.Parameters.AddWithValue("datetimeserved", datetimeserved);
                    Console.WriteLine("[LOG] Executing papers insert...");
                    int insertedRows = await cmdPapers.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] papers insert affected rows: {insertedRows}");
                    if (insertedRows == 0)
                        throw new Exception("Failed to insert into papers.");
                }
                Console.WriteLine("[LOG] Committing first transaction...");
                await tx.CommitAsync();
                Console.WriteLine($"[INFO] papers row committed: serialnum={paperSerial}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in first transaction: {ex}");
                await tx.RollbackAsync();
                Console.WriteLine("[LOG] First transaction rolled back.");
                throw;
            }
        }

        // --- SECOND TRANSACTION: All related-table logic ---
        job.JobId = paperSerial.ToString();
        Console.WriteLine($"[INFO] New Job Serial Number: {job.JobId}");
        Console.WriteLine($"[LOG] Starting second transaction for job.JobId={job.JobId}");
        await using (var conn = new NpgsqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                // ✅ Update typewrit in plongs
                if (!string.IsNullOrWhiteSpace(job.TypeOfWrit))
                {
                    Console.WriteLine("➡ Ensuring plongs row exists for typewrit...");
                    // Check if row exists
                    bool exists = false;
                    await using (var checkCmd = new NpgsqlCommand("SELECT 1 FROM plongs WHERE serialnum = @jobId", conn, tx))
                    {
                        checkCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        using var reader = await checkCmd.ExecuteReaderAsync();
                        exists = await reader.ReadAsync();
                    }
                    if (exists)
                    {
                        // Update
                        await using (var cmd1 = new NpgsqlCommand(@"
                            UPDATE plongs SET
                                typewrit = @typeOfWrit
                            WHERE serialnum = @jobId", conn, tx))
                        {
                            cmd1.Parameters.AddWithValue("typeOfWrit", job.TypeOfWrit);
                            cmd1.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            await cmd1.ExecuteNonQueryAsync();
                            Console.WriteLine("✅ plongs.typewrit updated.");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated plongs.typewrit.");
                        }
                    }
                    else
                    {
                        // Insert
                        await using (var cmdInsert = new NpgsqlCommand(@"
                            INSERT INTO plongs (serialnum, typewrit, changenum)
                            VALUES (@jobId, @typeOfWrit, @changenum);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            cmdInsert.Parameters.AddWithValue("typeOfWrit", job.TypeOfWrit);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine("✅ plongs row inserted for typewrit.");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", "Inserted plongs row for typewrit.");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ℹ️ Skipping plongs update — job.TypeOfWrit is empty.");
                }


                // ✅ Update or insert court name
                if (!string.IsNullOrWhiteSpace(job.Court))
                {
                    Console.WriteLine("➡ Attempting to update court name...");

                    int? courtNum = null;

                    // Step 1: Get courtnum from nested tables
                    await using (var cmdGetCourt = new NpgsqlCommand(@"
        SELECT courtnum
        FROM cases
        WHERE serialnum = (
            SELECT caseserialnum
            FROM papers
            WHERE serialnum = @jobId
        );", conn, tx))
                    {
                        cmdGetCourt.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGetCourt.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["courtnum"] != DBNull.Value)
                        {
                            courtNum = Convert.ToInt32(reader["courtnum"]);
                        }
                        await reader.CloseAsync();
                    }

                    if (courtNum.HasValue)
                    {
                        Console.WriteLine($"✅ courtNum resolved: {courtNum.Value}");
                        await using (var cmdUpdateCourt = new NpgsqlCommand(@"
            UPDATE courts
            SET name = @courtName
            WHERE serialnum = @courtNum;", conn, tx))
                        {
                            cmdUpdateCourt.Parameters.AddWithValue("courtName", job.Court);
                            cmdUpdateCourt.Parameters.AddWithValue("courtNum", courtNum.Value);
                            int affectedRows = await cmdUpdateCourt.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine("✅ courts.name updated.");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated courts.name.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No courtnum found — inserting new court and linking to cases...");

                        // Step 2: Insert new court
                        int newCourtSerial = await GenerateNewSerialAsync("courts", conn, tx); // You must ensure this is async
                        await using (var cmdInsertCourt = new NpgsqlCommand(@"
            INSERT INTO courts (serialnum, name)
            VALUES (@serial, @name);", conn, tx))
                        {
                            cmdInsertCourt.Parameters.AddWithValue("serial", newCourtSerial);
                            cmdInsertCourt.Parameters.AddWithValue("name", job.Court);
                            int affectedRows = await cmdInsertCourt.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Inserted new court with serialnum = {newCourtSerial}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new court with serialnum = {newCourtSerial}.");
                        }

                        // Step 3: Link court to case
                        await using (var cmdUpdateCase = new NpgsqlCommand(@"
            UPDATE cases
            SET courtnum = @courtSerial
            WHERE serialnum = (
                SELECT caseserialnum
                FROM papers
                WHERE serialnum = @jobId
            );", conn, tx))
                        {
                            cmdUpdateCase.Parameters.AddWithValue("courtSerial", newCourtSerial);
                            cmdUpdateCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdUpdateCase.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine("✅ cases.courtnum linked to new court.");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Linked court to case.");
                        }
                    }
                }

                // ✅ DEFENDANT - Update or Insert into CASES
                if (!string.IsNullOrWhiteSpace(job.Defendant))
                {
                    Console.WriteLine("➡ Attempting to update cases.defend1...");

                    int? caseSerialForDefendant = null;

                    // Step 1: Get case serialnum from papers
                    await using (var cmdGetCase = new NpgsqlCommand(@"
        SELECT caseserialnum
        FROM papers
        WHERE serialnum = @jobId;", conn, tx))
                    {
                        cmdGetCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGetCase.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["caseserialnum"] != DBNull.Value)
                        {
                            caseSerialForDefendant = Convert.ToInt32(reader["caseserialnum"]);
                        }
                        await reader.CloseAsync();
                    }

                    if (caseSerialForDefendant.HasValue)
                    {
                        Console.WriteLine($"✅ caseserialnum resolved: {caseSerialForDefendant.Value}");
                        await using (var cmdUpdateDefendant = new NpgsqlCommand(@"
            UPDATE cases
            SET defend1 = @defendant
            WHERE serialnum = @caseSerial;", conn, tx))
                        {
                            cmdUpdateDefendant.Parameters.AddWithValue("defendant", job.Defendant);
                            cmdUpdateDefendant.Parameters.AddWithValue("caseSerial", caseSerialForDefendant.Value);
                            int affectedRows = await cmdUpdateDefendant.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ cases.defend1 updated to: {job.Defendant}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated cases.defend1.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No existing case found. Inserting new case and linking it...");

                        int newCaseSerial = await GenerateNewSerialAsync("cases", conn, tx);

                        await using (var cmdInsertCase = new NpgsqlCommand(@"
            INSERT INTO cases (serialnum, defend1)
            VALUES (@serial, @defendant);", conn, tx))
                        {
                            cmdInsertCase.Parameters.AddWithValue("serial", newCaseSerial);
                            cmdInsertCase.Parameters.AddWithValue("defendant", job.Defendant);
                            int affectedRows = await cmdInsertCase.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Inserted new case with serialnum = {newCaseSerial}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new case with serialnum = {newCaseSerial}.");
                        }

                        await using (var cmdLinkCase = new NpgsqlCommand(@"
            UPDATE papers
            SET caseserialnum = @caseSerial
            WHERE serialnum = @jobId;", conn, tx))
                        {
                            cmdLinkCase.Parameters.AddWithValue("caseSerial", newCaseSerial);
                            cmdLinkCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdLinkCase.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Linked new case to papers.serialnum = {job.JobId}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Linked new case to papers.");
                        }
                    }
                }

                // ✅ PLAINTIFF - Update or Insert into SERVEEDETAILS
                if (job.IsPlaintiffsEdited && !string.IsNullOrWhiteSpace(job.Plaintiffs))
                {
                    var parts = job.Plaintiffs.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var firstName = parts.Length > 0 ? parts[0] : "";
                    var lastName = parts.Length > 1 ? parts[1] : "";

                    Console.WriteLine("➡ Attempting to update serveedetails for Plaintiff...");

                    int? plianNum = null;


                    // Step 1: Get pliannum from papers
                    await using (var cmdGetPlian = new NpgsqlCommand(@"
        SELECT serialnum
        FROM papers
        WHERE serialnum = @jobId;", conn, tx))
                    {
                        cmdGetPlian.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGetPlian.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["serialnum"] != DBNull.Value)
                        {
                            plianNum = Convert.ToInt32(reader["serialnum"]);
                        }
                        await reader.CloseAsync();
                    }

                    if (plianNum.HasValue)
                    {
                        Console.WriteLine($"✅ pliannum resolved: {plianNum.Value}");
                        await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE serveedetails
            SET firstname = @firstName, lastname = @lastName
            WHERE serialnum = @serialnum AND seqnum = 1;", conn, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                            cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                            cmdUpdate.Parameters.AddWithValue("serialnum", plianNum.Value);
                            int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Plaintiffs (serveedetails) updated: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated serveedetails for plaintiffs.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No existing pliannum found — inserting new serveedetails and linking...");

                        int newPlianSerial = await GenerateNewSerialAsync("serveedetails", conn, tx);

                        await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO serveedetails (serialnum, seqnum, firstname, lastname, changenum)
            VALUES (@serial, 1, @firstName, @lastName, @changenum);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("serial", newPlianSerial);
                            cmdInsert.Parameters.AddWithValue("firstName", firstName);
                            cmdInsert.Parameters.AddWithValue("lastName", lastName);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Inserted new serveedetails row with serialnum = {newPlianSerial}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new serveedetails row with serialnum = {newPlianSerial}.");
                        }
                        await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers
            SET pliannum = @plianSerial
            WHERE serialnum = @jobId;", conn, tx))
                        {
                            cmdLink.Parameters.AddWithValue("plianSerial", newPlianSerial);
                            cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Linked serveedetails.pliannum = {newPlianSerial} to papers.serialnum = {job.JobId}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Linked serveedetails to papers.");
                        }
                    }
                }
                // ✅ Attorney Update
                if (!string.IsNullOrWhiteSpace(job.Attorney))
                {
                    var parts = job.Attorney.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var firstName = parts.Length > 0 ? parts[0] : "";
                    var lastName = parts.Length > 1 ? parts[1] : "";

                    Console.WriteLine("➡ Attempting to update Attorney (entity)...");

                    int? attorneySerial = null;

                    await using (var cmdGet = new NpgsqlCommand("SELECT attorneynum FROM papers WHERE serialnum = @jobId;", conn, tx))
                    {
                        cmdGet.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGet.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["attorneynum"] != DBNull.Value)
                            attorneySerial = Convert.ToInt32(reader["attorneynum"]);
                        await reader.CloseAsync();
                    }

                    if (attorneySerial.HasValue)
                    {
                        Console.WriteLine($"✅ attorneynum resolved: {attorneySerial.Value}");

                        await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE entity SET ""FirstName"" = @firstName, ""LastName"" = @lastName
            WHERE ""SerialNum"" = @serial;", conn, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                            cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                            cmdUpdate.Parameters.AddWithValue("serial", attorneySerial.Value);
                            int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Attorney updated: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated attorney in entity.");
                        }
                    }
                    else
                    {

                        Console.WriteLine("⚠️ No attorneynum found — inserting and linking...");
                        int newSerial = await GenerateNewSerialAsync("entity", conn, tx);

                        await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"", ""ChangeNum"")
            VALUES (@serial, @firstName, @lastName, @changenum);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("serial", newSerial);
                            cmdInsert.Parameters.AddWithValue("firstName", firstName);
                            cmdInsert.Parameters.AddWithValue("lastName", lastName);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Attorney entity inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", "Inserted and linked new attorney entity.");
                        }

                        await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET attorneynum = @serial WHERE serialnum = @jobId;", conn, tx))
                        {
                            cmdLink.Parameters.AddWithValue("serial", newSerial);
                            cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Attorney entity inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated attorney in entity.");
                        }

                        Console.WriteLine($"✅ Attorney entity inserted and linked: {firstName} {lastName}");
                    }
                }

                // ✅ Client Update
                if (!string.IsNullOrWhiteSpace(job.Client))
                {
                    var parts = job.Client.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var firstName = parts.Length > 0 ? parts[0] : "";
                    var lastName = parts.Length > 1 ? parts[1] : "";

                    Console.WriteLine("➡ Attempting to update Client (entity)...");

                    int? clientSerial = null;
                    await using (var cmdGet = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId;", conn, tx))
                    {
                        cmdGet.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGet.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["clientnum"] != DBNull.Value)
                            clientSerial = Convert.ToInt32(reader["clientnum"]);
                        await reader.CloseAsync();
                    }

                    if (clientSerial.HasValue)
                    {
                        Console.WriteLine($"✅ clientnum resolved: {clientSerial.Value}");
                        await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE entity SET ""FirstName"" = @firstName, ""LastName"" = @lastName
            WHERE ""SerialNum"" = @serial;", conn, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                            cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                            cmdUpdate.Parameters.AddWithValue("serial", clientSerial.Value);
                            int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Client updated: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated client in entity.");
                        }
                        // Update status if provided
                        if (!string.IsNullOrWhiteSpace(job.ClientStatus))
                        {
                            await using (var cmdUpdateStatus = new NpgsqlCommand(@"
                                UPDATE entity SET ""status"" = @status WHERE ""SerialNum"" = @serial;", conn, tx))
                            {
                                cmdUpdateStatus.Parameters.AddWithValue("status", job.ClientStatus);
                                cmdUpdateStatus.Parameters.AddWithValue("serial", clientSerial.Value);
                                await cmdUpdateStatus.ExecuteNonQueryAsync();
                                Console.WriteLine($"✅ Client status updated: {job.ClientStatus}");
                                await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated client status in entity.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No clientnum found — inserting and linking...");
                        int newSerial = await GenerateNewSerialAsync("entity", conn, tx);
                        await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"", ""ChangeNum"", ""status"")
            VALUES (@serial, @firstName, @lastName, @changenum, @status);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("serial", newSerial);
                            cmdInsert.Parameters.AddWithValue("firstName", firstName);
                            cmdInsert.Parameters.AddWithValue("lastName", lastName);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            cmdInsert.Parameters.AddWithValue("status", job.ClientStatus ?? (object)DBNull.Value);
                            int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Client entity inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", "Inserted and linked new client entity.");
                        }

                        await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET clientnum = @serial WHERE serialnum = @jobId;", conn, tx))
                        {
                            cmdLink.Parameters.AddWithValue("serial", newSerial);
                            cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Client entity inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated client in entity.");
                        }

                        Console.WriteLine($"✅ Client entity inserted and linked: {firstName} {lastName}");
                    }
                }

                // ✅ Process Server Update
                if (!string.IsNullOrWhiteSpace(job.ProcessServer))
                {
                    var parts = job.ProcessServer.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var firstName = parts.Length > 0 ? parts[0] : "";
                    var lastName = parts.Length > 1 ? parts[1] : "";

                    Console.WriteLine("➡ Attempting to update Process Server...");

                    int? serverSerial = null;

                    await using (var cmdGet = new NpgsqlCommand("SELECT servercode FROM papers WHERE serialnum = @jobId;", conn, tx))
                    {
                        cmdGet.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGet.ExecuteReaderAsync();
                        if (await reader.ReadAsync() && reader["servercode"] != DBNull.Value)
                            serverSerial = Convert.ToInt32(reader["servercode"]);
                        await reader.CloseAsync();
                    }

                    if (serverSerial.HasValue)
                    {
                        Console.WriteLine($"✅ servercode resolved: {serverSerial.Value}");
                        await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE entity SET ""FirstName"" = @firstName, ""LastName"" = @lastName
            WHERE ""SerialNum"" = @serial;", conn, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                            cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                            cmdUpdate.Parameters.AddWithValue("serial", serverSerial.Value);
                            int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Process Server updated: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated process server in entity.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No servercode found — inserting and linking...");
                        int newSerial = await GenerateNewSerialAsync("entity", conn, tx);

                        await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"", ""ChangeNum"")
            VALUES (@serial, @firstName, @lastName, @changenum);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("serial", newSerial);
                            cmdInsert.Parameters.AddWithValue("firstName", firstName);
                            cmdInsert.Parameters.AddWithValue("lastName", lastName);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", "Inserted and linked new process server entity.");
                        }

                        await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET servercode = @serial WHERE serialnum = @jobId;", conn, tx))
                        {
                            cmdLink.Parameters.AddWithValue("serial", newSerial);
                            cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated process server in entity.");
                        }

                        Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                    }
                }

                // ✅ SERVEEDETAILS ADDRESS - Update or Insert into SERVEEDETAILS
                if (!string.IsNullOrWhiteSpace(job.Address))
                {
                    Console.WriteLine("➡ Attempting to update serveedetails address...");

                    // Parse the address components from the concatenated address
                    // Format: "Address1, City State Zip"
                    var parts = (job.Address ?? "").Split(',');
                    string address1 = parts.Length > 0 ? parts[0].Trim() : "";
                    string address2 = parts.Length > 1 ? parts[1].Trim() : "";
                    string city = parts.Length > 2 ? parts[2].Trim() : "";
                    string state = parts.Length > 3 ? parts[3].Trim() : "";
                    string zip = parts.Length > 4 ? parts[4].Trim() : "";

                    // Fallback for legacy data (if only one part and it looks like a full address)
                    if (parts.Length == 1 && string.IsNullOrWhiteSpace(address2 + city + state + zip))
                    {
                        // Try to parse using regex or string splitting (optional, for legacy support)
                    }

                    Console.WriteLine($"[DEBUG] Parsed address - Address1: '{address1}', City: '{city}', State: '{state}', Zip: '{zip}'");

                    // Check if serveedetails record exists for this job
                    int? serveeSerial = null;
                    await using (var cmdGetServee = new NpgsqlCommand(@"
        SELECT serialnum FROM serveedetails 
        WHERE serialnum = @jobId AND seqnum = 1;", conn, tx))
                    {
                        cmdGetServee.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var reader = await cmdGetServee.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            serveeSerial = Convert.ToInt32(reader["serialnum"]);
                        }
                        await reader.CloseAsync();
                    }

                    if (serveeSerial.HasValue)
                    {
                        Console.WriteLine($"✅ serveedetails record exists: {serveeSerial.Value}");
                        await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE serveedetails
            SET address1 = @address1, address2 = @address2, city = @city, state = @state, zip = @zip
            WHERE serialnum = @serveeSerial AND seqnum = 1;", conn, tx))
                        {
                            cmdUpdate.Parameters.AddWithValue("address1", address1);
                            cmdUpdate.Parameters.AddWithValue("address2", address2);
                            cmdUpdate.Parameters.AddWithValue("city", city);
                            cmdUpdate.Parameters.AddWithValue("state", state);
                            cmdUpdate.Parameters.AddWithValue("zip", zip);
                            cmdUpdate.Parameters.AddWithValue("serveeSerial", serveeSerial.Value);
                            int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Servee address updated: {address1}, {city} {state} {zip}");
                            await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Updated serveedetails address.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ No serveedetails record found — inserting new record...");
                        // Use the same serialnum as the job (job.JobId/paperSerial)
                        int serveeSerialToUse = int.Parse(job.JobId);
                        await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO serveedetails (serialnum, seqnum, address1, address2, city, state, zip, changenum)
            VALUES (@serial, 1, @address1, @address2, @city, @state, @zip, @changenum);", conn, tx))
                        {
                            cmdInsert.Parameters.AddWithValue("serial", serveeSerialToUse);
                            cmdInsert.Parameters.AddWithValue("address1", address1);
                            cmdInsert.Parameters.AddWithValue("address2", address2);
                            cmdInsert.Parameters.AddWithValue("city", city);
                            cmdInsert.Parameters.AddWithValue("state", state);
                            cmdInsert.Parameters.AddWithValue("zip", zip);
                            cmdInsert.Parameters.AddWithValue("changenum", 0); // or your versioning logic
                            int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ Inserted new serveedetails record with serialnum = {serveeSerialToUse}");
                            await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new serveedetails record with serialnum = {serveeSerialToUse}.");
                        }
                    }
                }

                // ✅ Service Type: Always insert a new typeservice row for a new job and link it
                if (!string.IsNullOrWhiteSpace(job.ServiceType))
                {
                    Console.WriteLine("➡ Inserting new ServiceType for new job...");
                    int newTypeSerial = await GenerateNewSerialAsync("typeservice", conn, tx);

                    await using (var cmdInsert = new NpgsqlCommand(@"
                        INSERT INTO typeservice (serialnumber, servicename, changenum)
                        VALUES (@serial, @name, @changenum);", conn, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newTypeSerial);
                        cmdInsert.Parameters.AddWithValue("name", job.ServiceType);
                        cmdInsert.Parameters.AddWithValue("changenum", 0);
                        await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Inserted new ServiceType with ID = {newTypeSerial}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new typeservice with ID = {newTypeSerial}.");
                    }

                    // Always update papers.typeservice
                    await using var cmdUpdateLink = new NpgsqlCommand(@"
                        UPDATE papers
                        SET typeservice = @serial
                        WHERE serialnum = @jobId;", conn, tx);
                    cmdUpdateLink.Parameters.AddWithValue("serial", newTypeSerial);
                    cmdUpdateLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await cmdUpdateLink.ExecuteNonQueryAsync();
                    Console.WriteLine("✅ papers.typeservice linked to new service.");
                    await LogAuditAsync(conn, tx, job.JobId, "UPDATE", "Linked typeservice to papers.");
                }

                // Build dictionary from memory using actual seqnum
                var incoming = job.Comments
                    .Where(c => c.Seqnum > 0)
                    .ToDictionary(c => c.Seqnum);

                // Step 1: Fetch existing comments from DB
                var existing = new Dictionary<int, string>();
                await using var connComments = new NpgsqlConnection(_connectionString);
                await connComments.OpenAsync();
                using (var cmd2 = new NpgsqlCommand(@"
    SELECT seqnum, comment
    FROM comments
    WHERE serialnum = @jobId AND isattempt = false;", connComments, tx))
                {
                    cmd2.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    using var reader = await cmd2.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int seq = Convert.ToInt32(reader["seqnum"]);
                        string comment = reader["comment"]?.ToString() ?? "";
                        existing[seq] = comment;
                    }
                }

                // Step 2: Insert or Update
                foreach (var c in job.Comments)
                {
                    long timestamp = ConvertToUnixTimestamp(c.Date, c.Time);

                    if (existing.ContainsKey(c.Seqnum))
                    {
                        // Update
                        using var update = new NpgsqlCommand(@"
            UPDATE comments
            SET comment = @comment,
                datetime = @datetime,
                source = @source,
                printonaff = @aff,
                printonfs = @fs,
                reviewed = @att
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn, tx);

                        update.Parameters.AddWithValue("comment", c.Body ?? "");
                        update.Parameters.AddWithValue("datetime", timestamp);
                        update.Parameters.AddWithValue("source", c.Source ?? "UI");
                        update.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                        update.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                        update.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;
                        update.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        update.Parameters.AddWithValue("seq", c.Seqnum);

                        await update.ExecuteNonQueryAsync();
                        Console.WriteLine($"🔄 Updated comment seq = {c.Seqnum}");
                        await LogAuditAsync(conn, tx, job.JobId, "UPDATE", $"Updated comment seq = {c.Seqnum}.");
                    }
                    else
                    {
                        // Insert new
                        using var insert = new NpgsqlCommand(@"
            INSERT INTO comments (
                serialnum, seqnum, changenum, comment, datetime, source,
                isattempt, printonaff, printonfs, reviewed)
            VALUES (
                @serialnum, @seqnum, 0, @comment, @datetime, @source,
                false, @aff, @fs, @att);", conn, tx);

                        insert.Parameters.AddWithValue("serialnum", long.Parse(job.JobId));
                        insert.Parameters.AddWithValue("seqnum", c.Seqnum);
                        insert.Parameters.AddWithValue("comment", c.Body ?? "");
                        insert.Parameters.AddWithValue("datetime", timestamp);
                        insert.Parameters.AddWithValue("source", c.Source ?? "UI");
                        insert.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                        insert.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                        insert.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;

                        await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"➕ Inserted comment seq = {c.Seqnum}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new comment seq = {c.Seqnum}.");
                    }
                }

                // Step 3: Delete removed comments
                var memorySeqs = new HashSet<int>(job.Comments.Select(c => c.Seqnum));
                foreach (var seq in existing.Keys)
                {
                    if (!memorySeqs.Contains(seq))
                    {
                        using var delete = new NpgsqlCommand(@"
            DELETE FROM comments
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn, tx);

                        delete.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        delete.Parameters.AddWithValue("seq", seq);

                        await delete.ExecuteNonQueryAsync();
                        Console.WriteLine($"🗑️ Deleted comment seq = {seq}");
                        await LogAuditAsync(conn, tx, job.JobId, "DELETE", $"Deleted comment seq = {seq}.");
                    }
                }


                // Step: Sync Attempts (exactly like Comments now)
                Console.WriteLine("🔄 Syncing attempts in DB...");

                var incomingAttempts = new Dictionary<int, AttemptsModel>();
                // Step: Assign new Seqnums to any new attempts
                // Step 1: Load existing attempts from DB
                var existingAttempts = new Dictionary<int, string>();
                int maxSeq = 1000;

                using (var cmd3 = new NpgsqlCommand(@"
    SELECT seqnum, comment
    FROM comments
    WHERE serialnum = @jobId AND isattempt = true;", conn, tx))
                {
                    cmd3.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    using var reader = await cmd3.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int existingSeq = Convert.ToInt32(reader["seqnum"]);
                        string body = reader["comment"]?.ToString() ?? "";
                        existingAttempts[existingSeq] = body;
                        if (existingSeq > maxSeq) maxSeq = existingSeq;
                    }
                }

                // Step 2: Assign new seqnum for new attempts
                var usedSeqs = new HashSet<int>(existingAttempts.Keys);

                foreach (var a in job.Attempts)
                {
                    if (a.Seqnum > 0)
                    {
                        incomingAttempts[a.Seqnum] = a;
                    }
                    else
                    {
                        do { maxSeq++; } while (usedSeqs.Contains(maxSeq));
                        a.Seqnum = maxSeq;
                        incomingAttempts[maxSeq] = a;
                        usedSeqs.Add(maxSeq);
                    }
                }


                using (var cmd4 = new NpgsqlCommand(@"
    SELECT seqnum, comment
    FROM comments
    WHERE serialnum = @jobId AND isattempt = true;", conn, tx))
                {
                    cmd4.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    using var reader = await cmd4.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int existingSeq = Convert.ToInt32(reader["seqnum"]);
                        string body = reader["comment"]?.ToString() ?? "";
                        existingAttempts[existingSeq] = body;
                    }
                }

                foreach (var kvp in incomingAttempts)
                {
                    var attemptSeq = kvp.Key;
                    var a = kvp.Value;
                    long ts = ConvertToUnixTimestamp(a.Date, a.Time);

                    if (existingAttempts.ContainsKey(attemptSeq))
                    {
                        using var update = new NpgsqlCommand(@"
            UPDATE comments SET
                comment = @comment,
                datetime = @datetime,
                source = @source,
                printonaff = @aff,
                printonfs = @fs,
                reviewed = @att
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = true;", conn, tx);

                        update.Parameters.AddWithValue("comment", a.Body ?? "");
                        update.Parameters.AddWithValue("datetime", ts);
                        update.Parameters.AddWithValue("source", a.Source ?? "UI");
                        update.Parameters.AddWithValue("aff", a.Aff ? 1 : 0);
                        update.Parameters.AddWithValue("fs", a.FS ? 1 : 0);
                        update.Parameters.AddWithValue("att", a.Att);
                        update.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        update.Parameters.AddWithValue("seq", attemptSeq);

                        int affectedRows = await update.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🔄 Updated attempt seq = {attemptSeq}");
                        await LogAuditAsync(conn, tx, job.JobId, "UPDATE", $"Updated attempt seq = {attemptSeq}.");
                    }
                    else
                    {
                        using var insert = new NpgsqlCommand(@"
            INSERT INTO comments (
                serialnum, seqnum, changenum, comment, datetime, source,
                isattempt, printonaff, printonfs, reviewed)
            VALUES (
                @serialnum, @seqnum, 0, @comment, @datetime, @source,
                true, @aff, @fs, @att);", conn, tx);

                        insert.Parameters.AddWithValue("serialnum", long.Parse(job.JobId));
                        insert.Parameters.AddWithValue("seqnum", attemptSeq);
                        insert.Parameters.AddWithValue("comment", a.Body ?? "");
                        insert.Parameters.AddWithValue("datetime", ts);
                        insert.Parameters.AddWithValue("source", a.Source ?? "UI");
                        insert.Parameters.AddWithValue("aff", a.Aff ? 1 : 0);
                        insert.Parameters.AddWithValue("fs", a.FS ? 1 : 0);
                        insert.Parameters.AddWithValue("att", a.Att);

                        int affectedRows = await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"➕ Inserted new attempt seq = {attemptSeq}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted new attempt seq = {attemptSeq}.");
                    }
                }

                // Step 4: Delete any attempts from DB that are no longer in memory
                var incomingAttemptSeqs = new HashSet<int>(incomingAttempts.Keys);
                foreach (var existingSeq in existingAttempts.Keys)
                {
                    if (!incomingAttemptSeqs.Contains(existingSeq))
                    {
                        using var delete = new NpgsqlCommand(@"
            DELETE FROM comments 
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = true;", conn, tx);

                        delete.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        delete.Parameters.AddWithValue("seq", existingSeq);
                        int affectedRows = await delete.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🗑️ Deleted attempt seq = {existingSeq}");
                        await LogAuditAsync(conn, tx, job.JobId, "DELETE", $"Deleted attempt seq = {existingSeq}.");
                    }
                }


                var existingInvoices = new Dictionary<Guid, InvoiceModel>();
                using (var cmd5 = new NpgsqlCommand(@"
    SELECT id, description, quantity, rate, amount 
    FROM joblineitem 
    WHERE jobnumber = @jobId;", conn, tx))
                {
                    cmd5.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    using var reader = await cmd5.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetGuid(0);
                        existingInvoices[id] = new InvoiceModel
                        {
                            Id = id,
                            Description = reader["description"]?.ToString() ?? "",
                            Quantity = Convert.ToInt32(reader["quantity"]),
                            Rate = Convert.ToDecimal(reader["rate"]),
                            Amount = Convert.ToDecimal(reader["amount"])
                        };
                    }
                }
                foreach (var inv in job.InvoiceEntries)
                {
                    if (inv.Id != Guid.Empty && existingInvoices.ContainsKey(inv.Id))
                    {
                        // Update existing invoice
                        using var update = new NpgsqlCommand(@"
            UPDATE joblineitem
            SET description = @desc, quantity = @qty, rate = @rate, amount = @amt
            WHERE id = @id;", conn, tx);

                        update.Parameters.AddWithValue("id", inv.Id);
                        update.Parameters.AddWithValue("desc", inv.Description);
                        update.Parameters.AddWithValue("qty", inv.Quantity);
                        update.Parameters.AddWithValue("rate", inv.Rate);
                        update.Parameters.AddWithValue("amt", inv.Amount);

                        int affectedRows = await update.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🔄 Updated invoice: {inv.Description}");
                        await LogAuditAsync(conn, tx, job.JobId, "UPDATE", $"Updated invoice: {inv.Description}.");
                    }
                    else
                    {
                        // Insert new invoice
                        var newId = inv.Id != Guid.Empty ? inv.Id : Guid.NewGuid();
                        using var insert = new NpgsqlCommand(@"
            INSERT INTO joblineitem (
                id, jobnumber, description, quantity, rate, amount,
                pricingmethod, ismanualrate, changenum)
            VALUES (
                @id, @jobId, @desc, @qty, @rate, @amt, 2, TRUE, 0);", conn, tx);

                        insert.Parameters.AddWithValue("id", newId);
                        insert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        insert.Parameters.AddWithValue("desc", inv.Description);
                        insert.Parameters.AddWithValue("qty", inv.Quantity);
                        insert.Parameters.AddWithValue("rate", inv.Rate);
                        insert.Parameters.AddWithValue("amt", inv.Amount);

                        int affectedRows = await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"➕ Inserted invoice: {inv.Description}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted invoice: {inv.Description}.");
                    }
                }
                Console.WriteLine("🔄 Syncing payments...");

                // 🗑️ Step 1: Delete payment if flagged
                if (job.DeletedPaymentId.HasValue)
                {
                    using var delete = new NpgsqlCommand("DELETE FROM payment WHERE id = @id", conn, tx);
                    delete.Parameters.AddWithValue("id", job.DeletedPaymentId.Value);
                    int affectedRows = await delete.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine($"🗑️ Deleted payment by ID: {job.DeletedPaymentId.Value}");
                    job.DeletedPaymentId = null; // Clear flag
                    await LogAuditAsync(conn, tx, job.JobId, "DELETE", $"Deleted payment by ID: {job.DeletedPaymentId.Value}.");
                }

                // ✅ Step 2: Load existing payments from DB
                var existingPayments = new Dictionary<Guid, PaymentModel>();
                using (var cmd6 = new NpgsqlCommand(@"
    SELECT id, date, method, description, amount 
    FROM payment 
    WHERE jobnumber = @jobId;", conn, tx))
                {
                    cmd6.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    using var reader = await cmd6.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetGuid(0);
                        existingPayments[id] = new PaymentModel
                        {
                            Id = id,
                            Date = Convert.ToDateTime(reader["date"]).Date,
                            TimeOnly = Convert.ToDateTime(reader["date"]).ToString("HH:mm:ss"),
                            Method = reader["method"]?.ToString(),
                            Description = reader["description"]?.ToString() ?? "",
                            Amount = Convert.ToDecimal(reader["amount"])
                        };
                    }
                }

                // ✅ Step 3: Insert or Update each payment entry
                foreach (var pay in job.Payments)
                {
                    if (job.DeletedPaymentId.HasValue && pay.Id == job.DeletedPaymentId.Value)
                        continue; // Skip deleted item

                    DateTime fullDateTime = DateTime.ParseExact($"{pay.Date:yyyy-MM-dd} {pay.TimeOnly}", "yyyy-MM-dd HH:mm:ss", null);

                    if (pay.Id != Guid.Empty && existingPayments.ContainsKey(pay.Id))
                    {
                        using var update = new NpgsqlCommand(@"
            UPDATE payment
            SET date = @dt, method = @method, description = @desc, amount = @amt
            WHERE id = @id;", conn, tx);

                        update.Parameters.AddWithValue("id", pay.Id);
                        update.Parameters.AddWithValue("dt", fullDateTime);
                        update.Parameters.AddWithValue("method", Convert.ToInt16(pay.Method ?? "0"));
                        update.Parameters.AddWithValue("desc", pay.Description ?? "");
                        update.Parameters.AddWithValue("amt", pay.Amount);

                        int affectedRows = await update.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🔄 Updated payment: {pay.Description}");
                        await LogAuditAsync(conn, tx, job.JobId, "UPDATE", $"Updated payment: {pay.Description}.");
                    }
                    else
                    {
                        var newId = pay.Id != Guid.Empty ? pay.Id : Guid.NewGuid();

                        using var insert = new NpgsqlCommand(@"
            INSERT INTO payment (id, jobnumber, date, method, description, amount, changenum)
            VALUES (@id, @jobId, @dt, @method, @desc, @amt, 0);", conn, tx);

                        insert.Parameters.AddWithValue("id", newId);
                        insert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        insert.Parameters.AddWithValue("dt", fullDateTime);
                        insert.Parameters.AddWithValue("method", Convert.ToInt16(pay.Method ?? "0"));
                        insert.Parameters.AddWithValue("desc", pay.Description ?? "");
                        insert.Parameters.AddWithValue("amt", pay.Amount);

                        int affectedRows = await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"➕ Inserted payment: {pay.Description}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted payment: {pay.Description}.");
                    }
                }

                // Step 1: DELETE (one at a time)
                // ✅ Step 4: Delete attachment if flagged
                if (job.DeletedAttachmentId.HasValue)
                {
                    await using var conn80 = new NpgsqlConnection(_connectionString);
                    await conn80.OpenAsync();
                    using var delete = new NpgsqlCommand("DELETE FROM attachments WHERE id = @id", conn80, tx);
                    delete.Parameters.AddWithValue("id", job.DeletedAttachmentId.Value);
                    int affectedRows = await delete.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine($"🗑️ Deleted attachment ID: {job.DeletedAttachmentId.Value}");
                    job.DeletedAttachmentId = null;
                    await LogAuditAsync(conn, tx, job.JobId, "DELETE", $"Deleted attachment ID: {job.DeletedAttachmentId.Value}.");
                }

                // Step // Step 2: INSERT or UPDATE attachments
                foreach (var att in job.Attachments)
                {
                    if (att.Status == "New")
                    {
                        Console.WriteLine($"[DEBUG] Inserting new attachment: ID={att.Id}, Filename={att.Filename}, Description={att.Description}, Format={att.Format}, Purpose={att.Purpose}");

                        using var insertAttachment = new NpgsqlCommand(@"
    INSERT INTO attachments (id, blobid, description, purpose, filename, changenum)
    VALUES (@id, @blobid, @desc, @purpose, @filename, @changenum);", conn, tx);

                        insertAttachment.Parameters.AddWithValue("id", att.Id);
                        insertAttachment.Parameters.AddWithValue("blobid", att.BlobId);
                        insertAttachment.Parameters.AddWithValue("desc", att.Description ?? "");
                        insertAttachment.Parameters.AddWithValue("purpose", int.TryParse(att.Purpose, out var p) ? p : 1);
                        insertAttachment.Parameters.AddWithValue("filename", att.Filename ?? "");
                        insertAttachment.Parameters.AddWithValue("changenum", 0); // or generate a value if needed

                        int affectedRows = await insertAttachment.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Inserted attachment: ID={att.Id}, Rows affected: {affectedRows}");
                        await LogAuditAsync(conn, tx, job.JobId, "INSERT", $"Inserted attachment: {att.Description}.");

                        // Optionally insert into blobmetadata, blobs, and papersattachmentscross as needed
                        att.Status = "Synced";

                        using var insertCross = new NpgsqlCommand(@"
    INSERT INTO papersattachmentscross (id, paperserialnum, attachmentid, changenum)
    VALUES (@id, @paperserialnum, @attachmentid, @changenum);", conn, tx);

                        insertCross.Parameters.AddWithValue("id", Guid.NewGuid());
                        insertCross.Parameters.AddWithValue("paperserialnum", long.Parse(job.JobId));
                        insertCross.Parameters.AddWithValue("attachmentid", att.Id);
                        insertCross.Parameters.AddWithValue("changenum", 0); // or a real value if needed

                        await insertCross.ExecuteNonQueryAsync();

                        using var insertBlobMeta = new NpgsqlCommand(@"
    INSERT INTO blobmetadata (id, changenum, fileextension)
    VALUES (@id, @changenum, @fileextension)
    ON CONFLICT (id) DO NOTHING;", conn, tx);

                        insertBlobMeta.Parameters.AddWithValue("id", att.BlobId);
                        insertBlobMeta.Parameters.AddWithValue("changenum", 0); // or a real value if needed
                        insertBlobMeta.Parameters.AddWithValue("fileextension", att.Format ?? "");

                        await insertBlobMeta.ExecuteNonQueryAsync();
                    }
                    else if (att.Status == "Edited")
                    {
                        Console.WriteLine($"[DEBUG] Updating attachment: ID={att.Id}, Description={att.Description}, Format={att.Format}, Purpose={att.Purpose}");
                        // Update attachments table
                        using var updateAttachment = new NpgsqlCommand(@"
            UPDATE attachments
            SET description = @desc, purpose = @purpose, format = @format
            WHERE id = @id;", conn, tx);
                        updateAttachment.Parameters.AddWithValue("id", att.Id);
                        updateAttachment.Parameters.AddWithValue("desc", att.Description ?? "");
                        updateAttachment.Parameters.AddWithValue("purpose", int.TryParse(att.Purpose, out var p) ? p : 1); // or your mapping
                        updateAttachment.Parameters.AddWithValue("format", att.Format ?? "");
                        int affectedRows = await updateAttachment.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Attachments table updated, rows affected: {affectedRows}");

                        // Optionally update blobmetadata if format changed
                        using var updateBlobMeta = new NpgsqlCommand(@"
            UPDATE blobmetadata
            SET fileextension = @ext
            WHERE id = @blobid;", conn, tx);
                        updateBlobMeta.Parameters.AddWithValue("ext", att.Format ?? "");
                        updateBlobMeta.Parameters.AddWithValue("blobid", att.BlobId);
                        int blobMetaRows = await updateBlobMeta.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Blobmetadata table updated, rows affected: {blobMetaRows}");

                        att.Status = "Synced"; // Reset status
                        Console.WriteLine($"[DEBUG] Attachment status set to Synced for ID={att.Id}");
                    }
                }



                // ✅ Clear deletion flag only AFTER all logic
                // ✅ Clear deletion flags AFTER all logic
                job.DeletedPaymentId = null;
                job.DeletedAttachmentId = null;


                if (!string.IsNullOrWhiteSpace(job.Plaintiff))
                {
                    // ✅ Update Plaintiff in entity table using caseserialnum from papers
                    string caseSerialNum = null;
                    await using (var connCase = new NpgsqlConnection(_connectionString))
                    {
                        await connCase.OpenAsync();
                        await using (var cmdCase = new NpgsqlCommand("SELECT pliannum FROM papers WHERE serialnum = @jobId", connCase))
                        {
                            cmdCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            await using var readerCase = await cmdCase.ExecuteReaderAsync();
                            if (await readerCase.ReadAsync())
                            {
                                caseSerialNum = readerCase["pliannum"]?.ToString();
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(job.Plaintiff) && int.TryParse(caseSerialNum, out int serialNum))
                    {
                        var nameParts = job.Plaintiff.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        string firstName = nameParts.Length > 0 ? nameParts[0] : "";
                        string lastName = nameParts.Length > 1 ? nameParts[1] : "";

                        await using (var cmd8 = new NpgsqlCommand(@"
                        UPDATE entity
                        SET ""FirstName"" = @firstName, ""LastName"" = @lastName
                        WHERE ""SerialNum"" = @caseSerialNum;", conn, tx))
                        {
                            cmd8.Parameters.AddWithValue("firstName", firstName);
                            cmd8.Parameters.AddWithValue("lastName", lastName);
                            cmd8.Parameters.AddWithValue("caseSerialNum", int.Parse(caseSerialNum));
                            int affectedRows = await cmd8.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ entity updated for plaintiff: {firstName} {lastName} (SerialNum: {serialNum})");
                        }
                        Console.WriteLine($"✅ entity updated for plaintiff: {firstName} {lastName} (SerialNum: {serialNum})");
                    }
                }

                Console.WriteLine("🟢 Committing transaction...");
                Console.WriteLine("✅ Job successfully saved.");
                Console.WriteLine("[LOG] Committing second transaction...");
                await tx.CommitAsync();
                Console.WriteLine("[LOG] Second transaction committed. Job successfully saved.");
                await LogAuditAsync(conn, tx, job.JobId, "COMMIT", "Committed transaction for SaveJob.");

            } // <-- End of try block for the second transaction
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in second transaction: {ex}");
                await tx.RollbackAsync();
                Console.WriteLine("[LOG] Second transaction rolled back.");
                throw;
            }
        } // <-- End of using (var conn = new NpgsqlConnection(_connectionString))
    }
    // Generates a new serial number for the specified table
    private static async Task<int> GenerateNewSerialAsync(string tableName, NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        string serialCol = tableName switch
        {
            "entity" => "\"SerialNum\"",
            "typeservice" => "serialnumber",
            _ => "serialnum"
        };
        using var cmd = new NpgsqlCommand($"SELECT COALESCE(MAX({serialCol}), 1000000) + 1 FROM {tableName};", conn, tx);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // Helper to generate new serial number for jobs (papers.serialnum), starting at 2025500000
    private static async Task<int> GenerateNewJobSerialAsync(NpgsqlConnection conn, NpgsqlTransaction tx)
    {
        int minSerial = 2025500000;
        using var cmd = new NpgsqlCommand($@"
        SELECT GREATEST(COALESCE(MAX(serialnum), {minSerial - 1}) + 1, {minSerial}) FROM papers;", conn, tx);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // Helper to parse a string to DateTime or return DBNull.Value if null/empty
    private static object ParseDateTimeOrNull(string dateTimeString)
    {
        if (string.IsNullOrWhiteSpace(dateTimeString))
            return DBNull.Value;
        if (DateTime.TryParse(dateTimeString, out var dt))
            return dt;
        return DBNull.Value;
    }
    
    
    private static long ConvertToUnixTimestamp(string date, string time)
    {
        if (DateTime.TryParse($"{date} {time}", out var dt))
        {
            var offset = new DateTimeOffset(dt);
            return offset.ToUnixTimeSeconds(); // safe conversion
        }
        return 0;
    }

     async Task LogAuditAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string jobId, string action, string details)
    {
        using var auditCmd = new NpgsqlCommand(@"
        INSERT INTO changehistory (jobid, action, username, details)
        VALUES (@jobid, @action, @username, @details);", conn, tx);

        auditCmd.Parameters.AddWithValue("jobid", long.Parse(jobId));
        auditCmd.Parameters.AddWithValue("action", action);
        auditCmd.Parameters.AddWithValue("username", SessionManager.CurrentUser?.LoginName ?? "Unknown");
        auditCmd.Parameters.AddWithValue("details", details);

        await auditCmd.ExecuteNonQueryAsync();
    }

}
