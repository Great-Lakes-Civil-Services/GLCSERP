using System;
using Npgsql;
using CivilProcessERP.Models.Job;
using static CivilProcessERP.Models.Job.InvoiceModel;
using CivilProcessERP.Models.Job; // Ensure this namespace contains PaymentEntryModel
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Controls; // Added for ListView
using System.IO; // Added for Path class
using System.Diagnostics; // Added for ProcessStartInfo
using System.Windows; // Added for MessageBox
using System.Windows.Threading;

public class JobService : INotifyPropertyChanged
{
    // Define AttachmentsListView as a property or field

    private static readonly SemaphoreSlim _jobLock = new(1, 1);
    public ListView AttachmentsListView { get; set; }
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public List<AttachmentModel> Attachments { get; set; } = new List<AttachmentModel>();


    public event PropertyChangedEventHandler PropertyChanged;


    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task<Job> GetJobById(string jobId)
    {
        Job job = new();

        //await _jobLock.WaitAsync(); // Acquire lock before reading
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            Console.WriteLine("[INFO] ✅ Connected to DB: " + conn.Database);

            // Step 1: Fetch from papers table
            await using var cmd1 = new NpgsqlCommand("SELECT serialnum, caseserialnum FROM papers WHERE serialnum = @jobId", conn);
            cmd1.Parameters.AddWithValue("jobId", long.Parse(jobId));

            await using var reader1 = await cmd1.ExecuteReaderAsync();

            if (!await reader1.ReadAsync())
            {
                Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
                return null;
            }

            job.JobId = reader1["serialnum"].ToString();
            var caseSerial = reader1["caseserialnum"].ToString();
            job.CaseNumber = caseSerial;

            // Step 2: Fetch Court number (courtnum) from cases table
            string courtNum = null;

            await using var conn1 = new NpgsqlConnection(_connectionString);
            await conn1.OpenAsync();
            await using (var cmd2 = new NpgsqlCommand("SELECT courtnum FROM cases WHERE serialnum = @caseserialnum", conn1))
            {
                cmd2.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
                await using var reader2 = await cmd2.ExecuteReaderAsync();
                if (await reader2.ReadAsync())
                {
                    courtNum = reader2["courtnum"]?.ToString();
                }
            }

            // Step 3: Fetch Court name from courts table based on courtnum
            if (!string.IsNullOrEmpty(courtNum))
            {
                await using var conn2 = new NpgsqlConnection(_connectionString);
                await conn2.OpenAsync();
                await using (var cmd24 = new NpgsqlCommand("SELECT name FROM courts WHERE serialnum = @courtnum", conn2))
                {
                    cmd24.Parameters.AddWithValue("courtnum", int.Parse(courtNum));
                    await using var reader24 = await cmd24.ExecuteReaderAsync();
                    if (await reader24.ReadAsync())
                    {
                        job.Court = reader24["name"]?.ToString(); // Assign the court name to the Job object
                    }
                }
                await using var conn3 = new NpgsqlConnection(_connectionString);
                await conn3.OpenAsync();
                // Step 4: Fetch Defendant from cases table
                await using (var cmd3 = new NpgsqlCommand("SELECT defend1 FROM cases WHERE serialnum = @caseserialnum", conn3))
                {
                    cmd3.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
                    await using var reader3 = await cmd3.ExecuteReaderAsync();
                    if (await reader3.ReadAsync())
                    {
                        job.Defendant = reader3["defend1"]?.ToString();
                    }
                }

                Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");

                await using var conn4 = new NpgsqlConnection(_connectionString);
                await conn4.OpenAsync();
                // Step 4: Fetch Zone
                await using (var cmd4 = new NpgsqlCommand("SELECT zone FROM papers WHERE serialnum = @jobId", conn4))
                {
                    cmd4.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader4 = await cmd4.ExecuteReaderAsync();
                    if (await reader4.ReadAsync())
                    {
                        job.Zone = reader4["zone"]?.ToString();
                    }
                }

                await using var conn5 = new NpgsqlConnection(_connectionString);
                await conn5.OpenAsync();
                // Step 5: SQL Received Date
                await using (var cmd5 = new NpgsqlCommand("SELECT sqldatetimerecd FROM papers WHERE serialnum = @jobId", conn5))
                {
                    cmd5.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader5 = await cmd5.ExecuteReaderAsync();
                    if (await reader5.ReadAsync())
                    {
                        job.SqlDateTimeCreated = reader5["sqldatetimerecd"]?.ToString();
                    }
                }

                await using var conn6 = new NpgsqlConnection(_connectionString);
                await conn6.OpenAsync();
                // Step 6: SQL Served Date
                await using (var cmd6 = new NpgsqlCommand("SELECT sqldatetimeserved FROM papers WHERE serialnum = @jobId", conn6))
                {
                    cmd6.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader6 = await cmd6.ExecuteReaderAsync();
                    if (await reader6.ReadAsync())
                    {
                        job.LastDayToServe = reader6["sqldatetimeserved"]?.ToString();
                    }
                }

                await using var conn7 = new NpgsqlConnection(_connectionString);
                await conn7.OpenAsync();
                // Step 7: Expiration Date
                await using (var cmd7 = new NpgsqlCommand("SELECT sqlexpiredate FROM papers WHERE serialnum = @jobId", conn7))
                {
                    cmd7.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader7 = await cmd7.ExecuteReaderAsync();
                    if (await reader7.ReadAsync())
                    {
                        job.ExpirationDate = reader7["sqlexpiredate"]?.ToString();
                    }
                }

                // Step 8: Get typeservice from papers
                string typeServiceId = null;
                await using var conn8 = new NpgsqlConnection(_connectionString);
                await conn8.OpenAsync();
                await using (var cmd8 = new NpgsqlCommand("SELECT typeservice FROM papers WHERE serialnum = @jobId", conn8))
                {
                    cmd8.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader8 = await cmd8.ExecuteReaderAsync();
                    if (await reader8.ReadAsync())
                    {
                        typeServiceId = reader8["typeservice"]?.ToString();
                    }
                }
                await using var conn9 = new NpgsqlConnection(_connectionString);
                await conn9.OpenAsync();

                // Step 9: Get servicename from typeservice table
                if (!string.IsNullOrEmpty(typeServiceId))
                {
                    await using (var cmd9 = new NpgsqlCommand("SELECT servicename FROM typeservice WHERE serialnumber = @typeservice", conn9))
                    {
                        cmd9.Parameters.AddWithValue("typeservice", int.Parse(typeServiceId));
                        await using var reader9 = await cmd9.ExecuteReaderAsync();
                        if (await reader9.ReadAsync())
                        {
                            job.ServiceType = reader9["servicename"]?.ToString();
                        }
                    }
                }
                // Step 10: Get caseserialnum from papers
                string caseSerialNum = null;
                await using var conn10 = new NpgsqlConnection(_connectionString);
                await conn10.OpenAsync();
                await using (var cmd10 = new NpgsqlCommand("SELECT caseserialnum FROM papers WHERE serialnum = @jobId", conn10))
                {
                    cmd10.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader10 = await cmd10.ExecuteReaderAsync();
                    if (await reader10.ReadAsync())
                    {
                        caseSerialNum = reader10["caseserialnum"]?.ToString();
                    }
                }

                await using var conn11 = new NpgsqlConnection(_connectionString);
                await conn11.OpenAsync();
                // Step 11: Get casenum from cases table
                if (!string.IsNullOrEmpty(caseSerialNum))
                {
                    await using (var cmd11 = new NpgsqlCommand("SELECT casenum FROM cases WHERE serialnum = @caseserialnum", conn11))
                    {
                        cmd11.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerialNum));
                        await using var reader11 = await cmd11.ExecuteReaderAsync();
                        if (await reader11.ReadAsync())
                        {
                            job.CaseNumber = reader11["casenum"]?.ToString();  // ✅ correctly assign
                        }
                    }
                }

                // Step 12: Get clientrefnum from papers
                await using var conn12 = new NpgsqlConnection(_connectionString);
                await conn12.OpenAsync();
                await using (var cmd12 = new NpgsqlCommand("SELECT clientrefnum FROM papers WHERE serialnum = @jobId", conn12))
                {
                    cmd12.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader12 = await cmd12.ExecuteReaderAsync();
                    if (await reader12.ReadAsync())
                    {
                        job.ClientReference = reader12["clientrefnum"]?.ToString();  // ✅ assign ClientReference
                    }
                }

                await using var conn13 = new NpgsqlConnection(_connectionString);
                await conn13.OpenAsync();
                // Step 12: Get Plaintiff Name from entity table using serialnumber = case number
                if (!string.IsNullOrEmpty(caseSerialNum))
                {
                    await using (var cmd13 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\" FROM entity WHERE \"SerialNum\" = @caseSerialNum", conn13))
                    {
                        cmd13.Parameters.AddWithValue("caseSerialNum", int.Parse(caseSerialNum));
                        await using var reader13 = await cmd13.ExecuteReaderAsync();
                        if (await reader13.ReadAsync())
                        {
                            var first = reader13["FirstName"]?.ToString();
                            var last = reader13["LastName"]?.ToString();
                            job.Plaintiff = $"{first} {last}".Trim();  // ✅ assign full name
                        }
                    }
                }

                // Step 13: Get attorneynum from papers table
                string attorneySerial = null;
                await using var conn14 = new NpgsqlConnection(_connectionString);
                await conn14.OpenAsync();
                await using (var cmd14 = new NpgsqlCommand("SELECT attorneynum FROM papers WHERE serialnum = @jobId", conn14))
                {
                    cmd14.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader14 = await cmd14.ExecuteReaderAsync();
                    if (await reader14.ReadAsync())
                    {
                        attorneySerial = reader14["attorneynum"]?.ToString();
                    }
                }

                await using var conn15 = new NpgsqlConnection(_connectionString);
                await conn15.OpenAsync();
                // Step 14: Get Attorney's name from entity table
                if (!string.IsNullOrEmpty(attorneySerial))
                {
                    await using (var cmd15 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\" FROM entity WHERE \"SerialNum\" = @attorneySerial", conn15))
                    {
                        cmd15.Parameters.AddWithValue("attorneySerial", int.Parse(attorneySerial));
                        await using var reader15 = await cmd15.ExecuteReaderAsync();
                        if (await reader15.ReadAsync())
                        {
                            var first = reader15["FirstName"]?.ToString();
                            var last = reader15["LastName"]?.ToString();
                            job.Attorney = $"{first} {last}".Trim();  // ✅ assign full name
                        }
                    }
                }

                // Step 15: Get clientnum from papers table
                string clientSerial = null;
                await using var conn16 = new NpgsqlConnection(_connectionString);
                await conn16.OpenAsync();
                await using (var cmd16 = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId", conn16))
                {
                    cmd16.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader16 = await cmd16.ExecuteReaderAsync();
                    if (await reader16.ReadAsync())
                    {
                        clientSerial = reader16["clientnum"]?.ToString();
                    }
                }

                await using var conn17 = new NpgsqlConnection(_connectionString);
                await conn17.OpenAsync();

                // Step 16: Get Client's name from entity table
                if (!string.IsNullOrEmpty(clientSerial))
                {
                    await using (var cmd17 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\" FROM entity WHERE \"SerialNum\" = @clientSerial", conn17))
                    {
                        cmd17.Parameters.AddWithValue("clientSerial", int.Parse(clientSerial));
                        await using var reader17 = await cmd17.ExecuteReaderAsync();
                        if (await reader17.ReadAsync())
                        {
                            var first = reader17["FirstName"]?.ToString();
                            var last = reader17["LastName"]?.ToString();
                            job.Client = $"{first} {last}".Trim();  // ✅ assign full client name
                        }
                    }
                }

                // Step 17: Get servercode from papers
                string serverCode = null;
                await using var conn18 = new NpgsqlConnection(_connectionString);
                await conn18.OpenAsync();
                await using (var cmd18 = new NpgsqlCommand("SELECT servercode FROM papers WHERE serialnum = @jobId", conn18))
                {
                    cmd18.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader18 = await cmd18.ExecuteReaderAsync();
                    if (await reader18.ReadAsync())
                    {
                        serverCode = reader18["servercode"]?.ToString();
                    }
                }
                // Step 18: Get Process Server name from entity table
                if (!string.IsNullOrEmpty(serverCode))
                {
                    await using var conn19 = new NpgsqlConnection(_connectionString);
                    await conn19.OpenAsync();
                    await using (var cmd19 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\" FROM entity WHERE \"SerialNum\" = @servercode", conn19))
                    {
                        cmd19.Parameters.AddWithValue("servercode", int.Parse(serverCode));
                        await using var reader19 = await cmd19.ExecuteReaderAsync();
                        if (await reader19.ReadAsync())
                        {
                            var first = reader19["FirstName"]?.ToString();
                            var last = reader19["LastName"]?.ToString();
                            job.ProcessServer = $"{first} {last}".Trim(); // full name
                        }
                    }
                }
                await using var conn20 = new NpgsqlConnection(_connectionString);
                await conn20.OpenAsync();
                // Step 19: Get Type of Writ from plongs
                await using (var cmd20 = new NpgsqlCommand("SELECT typewrit FROM plongs WHERE serialnum = @jobId", conn20))
                {
                    cmd20.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader20 = await cmd20.ExecuteReaderAsync();
                    if (await reader20.ReadAsync())
                    {
                        job.TypeOfWrit = reader20["typewrit"]?.ToString();
                    }
                }

                // Step 20 & 21: Get clientnum from papers → then status from entity
                string clientnum = null;
                await using var conn26 = new NpgsqlConnection(_connectionString);
                await conn26.OpenAsync();
                await using (var cmd26 = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId", conn26))
                {
                    cmd26.Parameters.AddWithValue("jobId", long.Parse(job.JobId));  // safely bind jobId
                    await using var reader26 = await cmd26.ExecuteReaderAsync();
                    if (await reader26.ReadAsync())
                    {
                        clientnum = reader26["clientnum"]?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(clientnum))
                {
                    await using var conn27 = new NpgsqlConnection(_connectionString);
                    await conn27.OpenAsync();
                    await using (var cmd21 = new NpgsqlCommand("SELECT \"status\" FROM entity WHERE \"SerialNum\" = @clientnum", conn27))
                    {
                        cmd21.Parameters.AddWithValue("clientnum", int.Parse(clientnum));
                        await using var reader21 = await cmd21.ExecuteReaderAsync();
                        if (await reader21.ReadAsync())
                        {
                            job.ClientStatus = reader21["status"]?.ToString();
                        }
                    }
                }
                // Step 22: Fetch courtdatecode and datetimeserved from papers
                string courtDateCodeRaw = null;
                string datetimeServedRaw = null;
                await using var conn22 = new NpgsqlConnection(_connectionString);
                await conn22.OpenAsync();
                await using (var cmd22 = new NpgsqlCommand("SELECT courtdatecode, datetimeserved FROM papers WHERE serialnum = @jobId", conn22))
                {
                    cmd22.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                    await using var reader22 = await cmd22.ExecuteReaderAsync();
                    if (await reader22.ReadAsync())
                    {
                        courtDateCodeRaw = reader22["courtdatecode"]?.ToString();
                        datetimeServedRaw = reader22["datetimeserved"]?.ToString();
                    }
                }

                // Debug: Show raw input
                Console.WriteLine($"Raw Value (courtdatecode): {courtDateCodeRaw}");

                // ✅ THE CORRECT OFFSET YOU DERIVED IN SQL
                const long timestampOffset = 1225178692;  // From your SQL calculation

                // --- COURT DATE ---
                if (long.TryParse(courtDateCodeRaw, out long courtTimestamp) && courtTimestamp != 0)
                {
                    var correctedCourtTimestamp = courtTimestamp + timestampOffset;
                    var courtDateTime = DateTimeOffset.FromUnixTimeSeconds(correctedCourtTimestamp).UtcDateTime;
                    Console.WriteLine($"Final UTC court date: {courtDateTime}");

                    job.Date = courtDateTime.ToString("MM/dd/yyyy");
                    job.Time = courtDateTime.ToString("h:mm tt");
                }
                else
                {
                    job.Date = "N/A";
                    job.Time = "N/A";
                }

                // --- DATETIME SERVED ---
                if (long.TryParse(datetimeServedRaw, out long servedTimestamp) && servedTimestamp != 0)
                {
                    var correctedServedTimestamp = servedTimestamp + timestampOffset;
                    var servedDateTime = DateTimeOffset.FromUnixTimeSeconds(correctedServedTimestamp).UtcDateTime;
                    Console.WriteLine($"Final UTC served date: {servedDateTime}");

                    job.ServiceDate = servedDateTime.ToString("MM/dd/yyyy");
                    job.ServiceTime = servedDateTime.ToString("h:mm tt");
                }
                else
                {
                    job.ServiceDate = "N/A";
                    job.ServiceTime = "N/A";
                }

                await using var conn23 = new NpgsqlConnection(_connectionString);
                await conn23.OpenAsync();
                // Step 23: Fetch joblineitem details
                await using (var cmd23 = new NpgsqlCommand(
                    "SELECT id, description, quantity::decimal, rate::decimal, amount::decimal FROM joblineitem WHERE jobnumber = @jobId", conn23))
                {
                    cmd23.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                    await using var reader23 = await cmd23.ExecuteReaderAsync();
                    while (await reader23.ReadAsync())
                    {
                        var invoiceItem = new InvoiceModel
                        {
                            Id = reader23.GetGuid(0), // 🆕 UUID
                            Description = reader23["description"]?.ToString() ?? "No Description",
                            Quantity = (int)(reader23["quantity"] != DBNull.Value ? Convert.ToDecimal(reader23["quantity"]) : 0),
                            Rate = reader23["rate"] != DBNull.Value ? Convert.ToDecimal(reader23["rate"]) : 0m,
                            Amount = reader23["amount"] != DBNull.Value ? Convert.ToDecimal(reader23["amount"]) : 0m
                        };

                        if (invoiceItem.Quantity == 0 || invoiceItem.Rate == 0 || invoiceItem.Amount == 0)
                        {
                            Console.WriteLine($"[INFO] Zero data found for description: {invoiceItem.Description}. Consider verifying database.");
                        }

                        job.InvoiceEntries.Add(invoiceItem);
                    }
                }

                OnPropertyChanged(nameof(job.TotalInvoiceAmount));
                OnPropertyChanged(nameof(job.TotalPaymentsAmount));

                await using var connPayments = new NpgsqlConnection(_connectionString);
                await connPayments.OpenAsync();
                // Step: Load Payments
                await using (var cmdPayment = new NpgsqlCommand("SELECT id, date, method, description, amount FROM payment WHERE jobnumber = @jobId", connPayments))
                {
                    cmdPayment.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var readerPayment = await cmdPayment.ExecuteReaderAsync();

                    while (await readerPayment.ReadAsync())
                    {
                        var dateTimeRaw = readerPayment["date"] != DBNull.Value
                            ? Convert.ToDateTime(readerPayment["date"])
                            : DateTime.MinValue;

                        string extractedDate = dateTimeRaw.ToString("yyyy-MM-dd");
                        string extractedTime = dateTimeRaw.ToString("HH:mm:ss");

                        var payment = new PaymentModel
                        {
                            Id = readerPayment.GetGuid(0),
                            Date = DateTime.Parse(extractedDate),
                            TimeOnly = extractedTime,
                            Method = readerPayment["method"]?.ToString(),
                            Description = readerPayment["description"]?.ToString(),
                            Amount = readerPayment["amount"] != DBNull.Value
                                ? Convert.ToDecimal(readerPayment["amount"])
                                : 0m
                        };

                        job.Payments.Add(payment);
                    }
                }

                // Shared timestamp offset
                // (removed duplicate declaration of timestampOffset)

                // Step: Load Comments
                await using var conn40 = new NpgsqlConnection(_connectionString);
                await conn40.OpenAsync();
                await using (var cmd = new NpgsqlCommand(@"SELECT 
                                            comment, datetime, source, isattempt, 
                                            printonaff, printonfs, reviewed 
                                            FROM comments 
                                            WHERE serialnum = @jobId AND isattempt = false", conn40))
                {
                    cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var comment = reader["comment"]?.ToString() ?? string.Empty;
                        var datetimeRaw = reader["datetime"];
                        DateTime datetimeParsed = DateTime.MinValue;

                        if (datetimeRaw != DBNull.Value)
                        {
                            if (datetimeRaw is int || datetimeRaw is long)
                            {
                                long rawTimestamp = Convert.ToInt64(datetimeRaw);
                                long correctedTimestamp = rawTimestamp + timestampOffset;
                                datetimeParsed = DateTimeOffset.FromUnixTimeSeconds(correctedTimestamp).UtcDateTime;
                            }
                            else
                            {
                                datetimeParsed = Convert.ToDateTime(datetimeRaw);
                            }
                        }

                        var date = datetimeParsed.ToString("yyyy-MM-dd");
                        var time = datetimeParsed.ToString("HH:mm:ss");

                        var source = reader["source"]?.ToString() ?? "Unknown Source";
                        bool affChecked = reader["printonaff"] != DBNull.Value && Convert.ToInt32(reader["printonaff"]) > 0;
                        bool dsChecked = reader["printonfs"] != DBNull.Value && Convert.ToInt32(reader["printonfs"]) > 0;
                        bool attChecked = reader["reviewed"] != DBNull.Value && Convert.ToBoolean(reader["reviewed"]);

                        var commentModel = new CommentModel
                        {
                            Date = date,
                            Time = time,
                            Body = comment,
                            Source = source,
                            Aff = affChecked,
                            FS = dsChecked,
                            Att = attChecked
                        };

                        job.Comments.Add(commentModel);
                    }
                }

                // Step: Load Attempts
                await using var conn25 = new NpgsqlConnection(_connectionString);
                await conn25.OpenAsync();
                await using (var cmd25 = new NpgsqlCommand(@"SELECT 
                                                comment, datetime, source, isattempt, 
                                                printonaff, printonfs, reviewed 
                                                FROM comments 
                                                WHERE serialnum = @jobId AND isattempt = true", conn25))
                {
                    cmd25.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader25 = await cmd25.ExecuteReaderAsync();

                    while (await reader25.ReadAsync())
                    {
                        var comment = reader25["comment"]?.ToString() ?? string.Empty;
                        var datetimeRaw = reader25["datetime"];
                        DateTime datetimeParsed = DateTime.MinValue;

                        if (datetimeRaw != DBNull.Value)
                        {
                            if (datetimeRaw is int || datetimeRaw is long)
                            {
                                long rawTimestamp = Convert.ToInt64(datetimeRaw);
                                long correctedTimestamp = rawTimestamp + timestampOffset;
                                datetimeParsed = DateTimeOffset.FromUnixTimeSeconds(correctedTimestamp).UtcDateTime;
                            }
                            else
                            {
                                datetimeParsed = Convert.ToDateTime(datetimeRaw);
                            }
                        }

                        var date = datetimeParsed.ToString("yyyy-MM-dd");
                        var time = datetimeParsed.ToString("HH:mm:ss");

                        var source = reader25["source"]?.ToString() ?? "Unknown Source";
                        bool affChecked = reader25["printonaff"] != DBNull.Value && Convert.ToInt32(reader25["printonaff"]) > 0;
                        bool dsChecked = reader25["printonfs"] != DBNull.Value && Convert.ToInt32(reader25["printonfs"]) > 0;
                        bool attChecked = reader25["reviewed"] != DBNull.Value && Convert.ToBoolean(reader25["reviewed"]);

                        var attemptsModel = new AttemptsModel
                        {
                            Date = date,
                            Time = time,
                            Body = comment,
                            Source = source,
                            Aff = affChecked,
                            FS = dsChecked,
                            Att = attChecked
                        };

                        job.Attempts.Add(attemptsModel);
                    }
                }

                string address1 = null, address2 = null, state = null, city = null, zip = null;

                await using var conn41 = new NpgsqlConnection(_connectionString);
                await conn41.OpenAsync();
                // Step: Fetch Servee Address Details
                await using (var cmd26 = new NpgsqlCommand("SELECT address1, address2, state, city, zip FROM serveedetails WHERE serialnum = @jobId AND seqnum = 1", conn41))
                {
                    cmd26.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader26 = await cmd26.ExecuteReaderAsync();
                    if (await reader26.ReadAsync())
                    {
                        address1 = reader26["address1"]?.ToString();
                        address2 = reader26["address2"]?.ToString();
                        state = reader26["state"]?.ToString();
                        city = reader26["city"]?.ToString();
                        zip = reader26["zip"]?.ToString();
                    }
                }

                await using var conn43 = new NpgsqlConnection(_connectionString);
                await conn43.OpenAsync();
                // Step: Fetch Plaintiffs (dudeservedlfm)
                await using (var cmd27 = new NpgsqlCommand("SELECT dudeservedlfm FROM papers WHERE serialnum = @jobId", conn43))
                {
                    cmd27.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader27 = await cmd27.ExecuteReaderAsync();
                    if (!await reader27.ReadAsync())
                    {
                        Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
                        return null;
                    }

                    job.Plaintiffs = reader27["dudeservedlfm"]?.ToString()?.Trim() ?? "";
                }

                // Step: Fetch Plaintiff Name from serveedetails if pliannum is available
                string pliannum = null;
                await using var conn28 = new NpgsqlConnection(_connectionString);
                await conn28.OpenAsync();
                await using (var cmd28 = new NpgsqlCommand("SELECT pliannum FROM papers WHERE serialnum = @jobId", conn28))
                {
                    cmd28.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader28 = await cmd28.ExecuteReaderAsync();
                    if (await reader28.ReadAsync())
                    {
                        pliannum = reader28["pliannum"]?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(pliannum))
                {
                    await using var conn29 = new NpgsqlConnection(_connectionString);
                    await conn29.OpenAsync();
                    await using var cmd29 = new NpgsqlCommand("SELECT \"firstname\", \"lastname\" FROM serveedetails WHERE \"serialnum\" = @pliannum AND \"seqnum\" = 1", conn29);
                    cmd29.Parameters.AddWithValue("pliannum", int.Parse(pliannum));
                    await using var reader29 = await cmd29.ExecuteReaderAsync();
                    if (await reader29.ReadAsync())
                    {
                        var firstName = reader29["firstname"]?.ToString()?.Trim();
                        var lastName = reader29["lastname"]?.ToString()?.Trim();
                        job.Plaintiff = $"{firstName} {lastName}".Trim();
                    }
                }

                // Step: Fetch Attachments
                await using var conn31 = new NpgsqlConnection(_connectionString);
                await conn31.OpenAsync();
                await using (var cmd31 = new NpgsqlCommand(@"SELECT 
    a.id AS attachment_id, 
    a.description, 
    a.purpose, 
    bm.fileextension, 
    bm.id AS blobmetadata_id 
    FROM attachments a 
    JOIN papersattachmentscross pac ON pac.attachmentid = a.id 
    JOIN blobmetadata bm ON bm.changenum = a.changenum 
    WHERE pac.paperserialnum = @serialnum;", conn31))
                {
                    int serialnum = Convert.ToInt32(jobId);
                    cmd31.Parameters.AddWithValue("serialnum", serialnum);
                    await using var reader31 = await cmd31.ExecuteReaderAsync();

                    while (await reader31.ReadAsync())
                    {
                        var purpose = reader31["purpose"]?.ToString();
                        var attachmentDescription = reader31["description"]?.ToString();
                        var fileExtension = reader31["fileextension"]?.ToString();
                        var blobMetadataId = reader31["blobmetadata_id"]?.ToString();

                        job.Attachments.Add(new AttachmentModel
                        {
                            Purpose = purpose,
                            Description = attachmentDescription ?? string.Empty,
                            Format = fileExtension,
                            BlobMetadataId = blobMetadataId
                        });
                    }
                }

                // Finalize: Set address
                job.Address = $"{address1} {address2} {state} {city} {zip}".Trim();
                Console.WriteLine($"Concatenated Address: {job.Address}");

                Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");
                return job;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] 🔥 DB Error: " + ex.Message);
            throw;
        }
        finally
        {
            //_jobLock.Release(); // Ensure lock is released even if an error occurs
        }
        return null;
    }
    public async Task SaveJob(Job job)
    {
        await using var conn32 = new NpgsqlConnection(_connectionString);
        await conn32.OpenAsync();

        await using var tx = await conn32.BeginTransactionAsync();

        try
        {
            Console.WriteLine("🔄 Starting SaveJobAsync for JobID: " + job.JobId);

            // Try UPDATE
            Console.WriteLine("➡ Attempting to update papers table...");
            await using (var cmd = new NpgsqlCommand(@"
            UPDATE papers SET
                zone = @zone,
                sqldatetimerecd = @sqlDateCreated,
                sqldatetimeserved = @lastDayToServe,
                sqlexpiredate = @expirationDate,
                clientrefnum = @clientRef
            WHERE serialnum = @jobId", conn32, tx))
            {
                cmd.Parameters.AddWithValue("zone", job.Zone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("sqlDateCreated", ParseDateTimeOrNull(job.SqlDateTimeCreated));
                cmd.Parameters.AddWithValue("lastDayToServe", ParseDateTimeOrNull(job.LastDayToServe));
                cmd.Parameters.AddWithValue("expirationDate", ParseDateTimeOrNull(job.ExpirationDate));
                cmd.Parameters.AddWithValue("clientRef", job.ClientReference ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected > 0)
                {
                    Console.WriteLine("✅ papers table updated.");
                }
                else
                {
                    Console.WriteLine("⚠️ No rows updated — checking if papers row exists...");

                    // Check existence
                    await using (var checkCmd = new NpgsqlCommand("SELECT 1 FROM papers WHERE serialnum = @jobId", conn32, tx))
                    {
                        checkCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        var exists = await checkCmd.ExecuteScalarAsync();

                        if (exists == null)
                        {
                            Console.WriteLine("❗ papers row does not exist — inserting minimal row...");

                            var fieldList = new List<string> { "serialnum" };
                            var valueList = new List<string> { "@jobId" };
                            await using var insertCmd = new NpgsqlCommand();
                            insertCmd.Connection = conn32;
                            insertCmd.Transaction = tx;

                            insertCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                            if (!string.IsNullOrWhiteSpace(job.Zone))
                            {
                                fieldList.Add("zone");
                                valueList.Add("@zone");
                                insertCmd.Parameters.AddWithValue("zone", job.Zone);
                            }
                            if (!string.IsNullOrWhiteSpace(job.SqlDateTimeCreated))
                            {
                                fieldList.Add("sqldatetimerecd");
                                valueList.Add("@sqlDateCreated");
                                insertCmd.Parameters.AddWithValue("sqlDateCreated", ParseDateTimeOrNull(job.SqlDateTimeCreated));
                            }
                            if (!string.IsNullOrWhiteSpace(job.LastDayToServe))
                            {
                                fieldList.Add("sqldatetimeserved");
                                valueList.Add("@lastDayToServe");
                                insertCmd.Parameters.AddWithValue("lastDayToServe", ParseDateTimeOrNull(job.LastDayToServe));
                            }
                            if (!string.IsNullOrWhiteSpace(job.ExpirationDate))
                            {
                                fieldList.Add("sqlexpiredate");
                                valueList.Add("@expirationDate");
                                insertCmd.Parameters.AddWithValue("expirationDate", ParseDateTimeOrNull(job.ExpirationDate));
                            }
                            if (!string.IsNullOrWhiteSpace(job.ClientReference))
                            {
                                fieldList.Add("clientrefnum");
                                valueList.Add("@clientRef");
                                insertCmd.Parameters.AddWithValue("clientRef", job.ClientReference);
                            }

                            if (fieldList.Count == 1)
                            {
                                Console.WriteLine("❌ No valid fields found to insert into papers — skipping INSERT.");
                            }
                            else
                            {
                                insertCmd.CommandText = $@"
                                INSERT INTO papers ({string.Join(", ", fieldList)})
                                VALUES ({string.Join(", ", valueList)});";

                                int inserted = await insertCmd.ExecuteNonQueryAsync();
                                if (inserted > 0)
                                    Console.WriteLine("✅ Minimal papers row inserted.");
                                else
                                    Console.WriteLine("❌ INSERT into papers failed — check constraints or values.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("ℹ️ papers row exists — no values changed, nothing inserted.");
                        }
                    }
                }
            }


            // ✅ Update typewrit in plongs
            if (!string.IsNullOrWhiteSpace(job.TypeOfWrit))
            {
                Console.WriteLine("➡ Updating plongs.typewrit...");
                await using (var cmd = new NpgsqlCommand(@"
        UPDATE plongs SET
            typewrit = @typeOfWrit
        WHERE serialnum = @jobId", conn32, tx))
                {
                    cmd.Parameters.AddWithValue("typeOfWrit", job.TypeOfWrit);
                    cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                        Console.WriteLine("✅ plongs.typewrit updated.");
                    else
                        Console.WriteLine("⚠️ No plongs row updated — serialnum may be missing.");
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
        );", conn32, tx))
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
            WHERE serialnum = @courtNum;", conn32, tx))
                    {
                        cmdUpdateCourt.Parameters.AddWithValue("courtName", job.Court);
                        cmdUpdateCourt.Parameters.AddWithValue("courtNum", courtNum.Value);
                        await cmdUpdateCourt.ExecuteNonQueryAsync();
                        Console.WriteLine("✅ courts.name updated.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No courtnum found — inserting new court and linking to cases...");

                    // Step 2: Insert new court
                    int newCourtSerial = await GenerateNewSerialAsync("courts", conn32, tx); // You must ensure this is async
                    await using (var cmdInsertCourt = new NpgsqlCommand(@"
            INSERT INTO courts (serialnum, name)
            VALUES (@serial, @name);", conn32, tx))
                    {
                        cmdInsertCourt.Parameters.AddWithValue("serial", newCourtSerial);
                        cmdInsertCourt.Parameters.AddWithValue("name", job.Court);
                        await cmdInsertCourt.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Inserted new court with serialnum = {newCourtSerial}");
                    }

                    // Step 3: Link court to case
                    await using (var cmdUpdateCase = new NpgsqlCommand(@"
            UPDATE cases
            SET courtnum = @courtSerial
            WHERE serialnum = (
                SELECT caseserialnum
                FROM papers
                WHERE serialnum = @jobId
            );", conn32, tx))
                    {
                        cmdUpdateCase.Parameters.AddWithValue("courtSerial", newCourtSerial);
                        cmdUpdateCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdUpdateCase.ExecuteNonQueryAsync();
                        Console.WriteLine("✅ cases.courtnum linked to new court.");
                    }
                }
            }

            // ✅ DEFENDANT - Update or Insert into CASES
            if (!string.IsNullOrWhiteSpace(job.Defendant))
            {
                Console.WriteLine("➡ Attempting to update cases.defend1...");

                int? caseSerial = null;

                // Step 1: Get case serialnum from papers
                await using (var cmdGetCase = new NpgsqlCommand(@"
        SELECT caseserialnum
        FROM papers
        WHERE serialnum = @jobId;", conn32, tx))
                {
                    cmdGetCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader = await cmdGetCase.ExecuteReaderAsync();
                    if (await reader.ReadAsync() && reader["caseserialnum"] != DBNull.Value)
                    {
                        caseSerial = Convert.ToInt32(reader["caseserialnum"]);
                    }
                    await reader.CloseAsync();
                }

                if (caseSerial.HasValue)
                {
                    Console.WriteLine($"✅ caseserialnum resolved: {caseSerial.Value}");
                    await using (var cmdUpdateDefendant = new NpgsqlCommand(@"
            UPDATE cases
            SET defend1 = @defendant
            WHERE serialnum = @caseSerial;", conn32, tx))
                    {
                        cmdUpdateDefendant.Parameters.AddWithValue("defendant", job.Defendant);
                        cmdUpdateDefendant.Parameters.AddWithValue("caseSerial", caseSerial.Value);
                        await cmdUpdateDefendant.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ cases.defend1 updated to: {job.Defendant}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No existing case found. Inserting new case and linking it...");

                    int newCaseSerial = await GenerateNewSerialAsync("cases", conn32, tx);

                    await using (var cmdInsertCase = new NpgsqlCommand(@"
            INSERT INTO cases (serialnum, defend1)
            VALUES (@serial, @defendant);", conn32, tx))
                    {
                        cmdInsertCase.Parameters.AddWithValue("serial", newCaseSerial);
                        cmdInsertCase.Parameters.AddWithValue("defendant", job.Defendant);
                        await cmdInsertCase.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Inserted new case with serialnum = {newCaseSerial}");
                    }

                    await using (var cmdLinkCase = new NpgsqlCommand(@"
            UPDATE papers
            SET caseserialnum = @caseSerial
            WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLinkCase.Parameters.AddWithValue("caseSerial", newCaseSerial);
                        cmdLinkCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdLinkCase.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Linked new case to papers.serialnum = {job.JobId}");
                    }
                }
            }

            // ✅ PLAINTIFF - Update or Insert into SERVEEDETAILS
            if (!string.IsNullOrWhiteSpace(job.Plaintiff))
            {
                var parts = job.Plaintiff.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var firstName = parts.Length > 0 ? parts[0] : "";
                var lastName = parts.Length > 1 ? parts[1] : "";

                Console.WriteLine("➡ Attempting to update serveedetails for Plaintiff...");

                int? plianNum = null;


                // Step 1: Get pliannum from papers
                await using (var cmdGetPlian = new NpgsqlCommand(@"
        SELECT pliannum
        FROM papers
        WHERE serialnum = @jobId;", conn32, tx))
                {
                    cmdGetPlian.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader = await cmdGetPlian.ExecuteReaderAsync();
                    if (await reader.ReadAsync() && reader["pliannum"] != DBNull.Value)
                    {
                        plianNum = Convert.ToInt32(reader["pliannum"]);
                    }
                    await reader.CloseAsync();
                }

                if (plianNum.HasValue)
                {
                    Console.WriteLine($"✅ pliannum resolved: {plianNum.Value}");
                    await using (var cmdUpdate = new NpgsqlCommand(@"
            UPDATE serveedetails
            SET firstname = @firstName, lastname = @lastName
            WHERE serialnum = @plianNum AND seqnum = 1;", conn32, tx))
                    {
                        cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                        cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                        cmdUpdate.Parameters.AddWithValue("plianNum", plianNum.Value);
                        await cmdUpdate.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Plaintiff (serveedetails) updated: {firstName} {lastName}");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No existing pliannum found — inserting new serveedetails and linking...");

                    int newPlianSerial = await GenerateNewSerialAsync("serveedetails", conn32, tx);

                    await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO serveedetails (serialnum, seqnum, firstname, lastname)
            VALUES (@serial, 1, @firstName, @lastName);", conn32, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newPlianSerial);
                        cmdInsert.Parameters.AddWithValue("firstName", firstName);
                        cmdInsert.Parameters.AddWithValue("lastName", lastName);
                        await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Inserted new serveedetails row with serialnum = {newPlianSerial}");
                    }
                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers
            SET pliannum = @plianSerial
            WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("plianSerial", newPlianSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdLink.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ Linked serveedetails.pliannum = {newPlianSerial} to papers.serialnum = {job.JobId}");
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

                await using (var cmdGet = new NpgsqlCommand("SELECT attorneynum FROM papers WHERE serialnum = @jobId;", conn32, tx))
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
            WHERE ""SerialNum"" = @serial;", conn32, tx))
                    {
                        cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                        cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                        cmdUpdate.Parameters.AddWithValue("serial", attorneySerial.Value);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"✅ Attorney updated: {firstName} {lastName}");
                }
                else
                {

                    Console.WriteLine("⚠️ No attorneynum found — inserting and linking...");
                    int newSerial = await GenerateNewSerialAsync("entity", conn32, tx);

                    await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"")
            VALUES (@serial, @firstName, @lastName);", conn32, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newSerial);
                        cmdInsert.Parameters.AddWithValue("firstName", firstName);
                        cmdInsert.Parameters.AddWithValue("lastName", lastName);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET attorneynum = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdLink.ExecuteNonQueryAsync();
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
                await using (var cmdGet = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId;", conn32, tx))
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
            WHERE ""SerialNum"" = @serial;", conn32, tx))
                    {
                        cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                        cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                        cmdUpdate.Parameters.AddWithValue("serial", clientSerial.Value);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"✅ Client updated: {firstName} {lastName}");
                }
                else
                {
                    Console.WriteLine("⚠️ No clientnum found — inserting and linking...");

                    int newSerial = await GenerateNewSerialAsync("entity", conn32, tx);


                    await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"")
            VALUES (@serial, @firstName, @lastName);", conn32, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newSerial);
                        cmdInsert.Parameters.AddWithValue("firstName", firstName);
                        cmdInsert.Parameters.AddWithValue("lastName", lastName);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET clientnum = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdLink.ExecuteNonQueryAsync();
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

                await using (var cmdGet = new NpgsqlCommand("SELECT servercode FROM papers WHERE serialnum = @jobId;", conn32, tx))
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
            WHERE ""SerialNum"" = @serial;", conn32, tx))
                    {
                        cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                        cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                        cmdUpdate.Parameters.AddWithValue("serial", serverSerial.Value);
                        await cmdUpdate.ExecuteNonQueryAsync();
                    }
                    Console.WriteLine($"✅ Process Server updated: {firstName} {lastName}");
                }
                else
                {
                    Console.WriteLine("⚠️ No servercode found — inserting and linking...");
                    int newSerial = await GenerateNewSerialAsync("entity", conn32, tx);

                    await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO entity (""SerialNum"", ""FirstName"", ""LastName"")
            VALUES (@serial, @firstName, @lastName);", conn32, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newSerial);
                        cmdInsert.Parameters.AddWithValue("firstName", firstName);
                        cmdInsert.Parameters.AddWithValue("lastName", lastName);
                        await cmdInsert.ExecuteNonQueryAsync();
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET servercode = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await cmdLink.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                }
            }
            if (!string.IsNullOrWhiteSpace(job.ServiceType))
            {
                Console.WriteLine("➡ Attempting to update ServiceType...");

                int? typeSerial = null;

                await using (var cmdGet = new NpgsqlCommand(@"
        SELECT typeservice
        FROM papers
        WHERE serialnum = @jobId;", conn32, tx))
                {
                    cmdGet.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader = await cmdGet.ExecuteReaderAsync();
                    if (await reader.ReadAsync() && reader["typeservice"] != DBNull.Value)
                    {
                        typeSerial = Convert.ToInt32(reader["typeservice"]);
                    }
                }

                if (typeSerial.HasValue)
                {
                    await using var cmdUpdate = new NpgsqlCommand(@"
            UPDATE typeservice
            SET servicename = @serviceName
            WHERE serialnumber = @serial;", conn32, tx);
                    cmdUpdate.Parameters.AddWithValue("serviceName", job.ServiceType);
                    cmdUpdate.Parameters.AddWithValue("serial", typeSerial.Value);
                    await cmdUpdate.ExecuteNonQueryAsync();
                    Console.WriteLine($"✅ ServiceType updated to: {job.ServiceType}");
                }
                else
                {
                    int newTypeSerial = await GenerateNewSerialAsync("typeservice", conn32, tx);
                    await using var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO typeservice (serialnumber, servicename)
            VALUES (@serial, @name);", conn32, tx);
                    cmdInsert.Parameters.AddWithValue("serial", newTypeSerial);
                    cmdInsert.Parameters.AddWithValue("name", job.ServiceType);
                    await cmdInsert.ExecuteNonQueryAsync();
                    Console.WriteLine($"✅ Inserted new ServiceType with ID = {newTypeSerial}");


                    await using var cmdUpdateLink = new NpgsqlCommand(@"
            UPDATE papers
            SET typeservice = @serial
            WHERE serialnum = @jobId;", conn32, tx);
                    cmdUpdateLink.Parameters.AddWithValue("serial", newTypeSerial);
                    cmdUpdateLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await cmdUpdateLink.ExecuteNonQueryAsync();
                    Console.WriteLine("✅ papers.typeservice linked to new service.");
                }
            }
            //q -> comment list from memory
            var incoming = new Dictionary<int, CommentModel>();
            int seq = 1;
            foreach (var comment in job.Comments)
            {
                incoming[seq++] = comment;
            }
            Console.WriteLine("🔄 Refreshing comments in DB...");

            // Step 1: Get existing comment seqnums
            var existing = new Dictionary<int, string>();
            await using var connComments = new NpgsqlConnection(_connectionString);
            await connComments.OpenAsync();
            using (var cmd = new NpgsqlCommand(@"
    SELECT seqnum, comment
    FROM comments
    WHERE serialnum = @jobId AND isattempt = false;", connComments, tx))
            {
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int existingSeq = Convert.ToInt32(reader["seqnum"]);
                    string body = reader["comment"]?.ToString() ?? "";
                    existing[existingSeq] = body;
                }
            }

            foreach (var kvp in incoming)
            {
                var currentSeq = kvp.Key;
                var c = kvp.Value;
                long timestamp = ConvertToUnixTimestamp(c.Date, c.Time);

                if (existing.ContainsKey(currentSeq))
                {
                    using var update = new NpgsqlCommand(@"
            UPDATE comments
            SET comment = @comment,
                datetime = @datetime,
                source = @source,
                printonaff = @aff,
                printonfs = @fs,
                reviewed = @att
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

                    update.Parameters.AddWithValue("comment", c.Body ?? "");
                    update.Parameters.AddWithValue("datetime", timestamp);
                    update.Parameters.AddWithValue("source", c.Source ?? "UI");
                    update.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                    update.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                    update.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;
                    update.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    update.Parameters.AddWithValue("seq", currentSeq);

                    await update.ExecuteNonQueryAsync();
                    Console.WriteLine($"🔄 Updated comment seq = {currentSeq}");
                }
                else
                {
                    using var insert = new NpgsqlCommand(@"
            INSERT INTO comments (
                serialnum, seqnum, changenum, comment, datetime, source,
                isattempt, printonaff, printonfs, reviewed)
            VALUES (
                @serialnum, @seqnum, 0, @comment, @datetime, @source,
                false, @aff, @fs, @att);", conn32, tx);

                    insert.Parameters.AddWithValue("serialnum", long.Parse(job.JobId));
                    insert.Parameters.AddWithValue("seqnum", currentSeq);
                    insert.Parameters.AddWithValue("comment", c.Body ?? "");
                    insert.Parameters.AddWithValue("datetime", timestamp);
                    insert.Parameters.AddWithValue("source", c.Source ?? "UI");
                    insert.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                    insert.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                    insert.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;

                    await insert.ExecuteNonQueryAsync();
                    Console.WriteLine($"➕ Inserted new comment seq = {currentSeq}");
                }
            }

            // Step 4: Delete rows that were removed in memory
            var incomingSeqs = new HashSet<int>(incoming.Keys);
            foreach (var existingSeq in existing.Keys)
            {
                if (!incomingSeqs.Contains(existingSeq))
                {
                    using var deleteCmd = new NpgsqlCommand(@"
            DELETE FROM comments
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

                    deleteCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    deleteCmd.Parameters.AddWithValue("seq", existingSeq);
                    await deleteCmd.ExecuteNonQueryAsync();

                    Console.WriteLine($"🗑️ Deleted comment seq = {existingSeq}");
                }
            }
            Console.WriteLine("🔄 Refreshing attempts in DB...");

            int maxAttemptSeq = 1000;

            using (var maxCmd = new NpgsqlCommand(@"
    SELECT COALESCE(MAX(seqnum), 999)
    FROM comments
    WHERE serialnum = @jobId AND isattempt = true;", conn32, tx))
            {
                maxCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                maxAttemptSeq = Convert.ToInt32(await maxCmd.ExecuteScalarAsync());
            }

            var existingAttempts = new Dictionary<int, string>();

            using (var cmd = new NpgsqlCommand(@"
    SELECT seqnum, comment
    FROM comments
    WHERE serialnum = @jobId AND isattempt = true;", conn32, tx))
            {
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int existingSeq = Convert.ToInt32(reader["seqnum"]);
                    string body = reader["comment"]?.ToString() ?? "";
                    existingAttempts[existingSeq] = body;
                }
            }
            var usedSeqs = new HashSet<int>(existingAttempts.Keys);
            var incomingAttempts = new Dictionary<int, AttemptsModel>();

            foreach (var attempt in job.Attempts)
            {
                do { maxAttemptSeq++; }
                while (usedSeqs.Contains(maxAttemptSeq));

                incomingAttempts[maxAttemptSeq] = attempt;
                usedSeqs.Add(maxAttemptSeq);
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
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = true;", conn32, tx);

                    update.Parameters.AddWithValue("comment", a.Body ?? "");
                    update.Parameters.AddWithValue("datetime", ts);
                    update.Parameters.AddWithValue("source", a.Source ?? "UI");
                    update.Parameters.AddWithValue("aff", a.Aff ? 1 : 0);
                    update.Parameters.AddWithValue("fs", a.FS ? 1 : 0);
                    update.Parameters.AddWithValue("att", a.Att);
                    update.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    update.Parameters.AddWithValue("seq", attemptSeq);

                    await update.ExecuteNonQueryAsync();
                    Console.WriteLine($"🔄 Updated attempt seq = {attemptSeq}");
                }
                else
                {

                    using var insert = new NpgsqlCommand(@"
            INSERT INTO comments (
                serialnum, seqnum, changenum, comment, datetime, source,
                isattempt, printonaff, printonfs, reviewed)
            VALUES (
                @serialnum, @seqnum, 0, @comment, @datetime, @source,
                true, @aff, @fs, @att);", conn32, tx);

                    insert.Parameters.AddWithValue("serialnum", long.Parse(job.JobId));
                    insert.Parameters.AddWithValue("seqnum", attemptSeq);
                    insert.Parameters.AddWithValue("comment", a.Body ?? "");
                    insert.Parameters.AddWithValue("datetime", ts);
                    insert.Parameters.AddWithValue("source", a.Source ?? "UI");
                    insert.Parameters.AddWithValue("aff", a.Aff ? 1 : 0);
                    insert.Parameters.AddWithValue("fs", a.FS ? 1 : 0);
                    insert.Parameters.AddWithValue("att", a.Att);

                    await insert.ExecuteNonQueryAsync();
                    Console.WriteLine($"➕ Inserted new attempt seq = {attemptSeq}");
                }
            }

            // Step 4: Delete any DB attempts that are no longer in memory
            var incomingAttemptSeqs = new HashSet<int>(incomingAttempts.Keys);
            foreach (var existingSeq in existingAttempts.Keys)
            {
                if (!incomingAttemptSeqs.Contains(existingSeq))
                {
                    using var delete = new NpgsqlCommand(@"
            DELETE FROM comments 
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = true;", conn32, tx);

                    delete.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    delete.Parameters.AddWithValue("seq", existingSeq);
                    await delete.ExecuteNonQueryAsync();
                    Console.WriteLine($"🗑️ Deleted attempt seq = {existingSeq}");
                }
            }

            Console.WriteLine("🔄 Syncing invoice entries...");

            // Handle deletion if flagged
            if (job.DeletedInvoiceId.HasValue)
            {
                using var delete = new NpgsqlCommand("DELETE FROM joblineitem WHERE id = @id", conn32, tx);
                delete.Parameters.AddWithValue("id", job.DeletedInvoiceId.Value);
                await delete.ExecuteNonQueryAsync();
                Console.WriteLine($"🗑️ Deleted invoice by ID: {job.DeletedInvoiceId.Value}");
                job.DeletedInvoiceId = null;
            }


            var existingInvoices = new Dictionary<Guid, InvoiceModel>();
            using (var cmd = new NpgsqlCommand(@"
    SELECT id, description, quantity, rate, amount 
    FROM joblineitem 
    WHERE jobnumber = @jobId;", conn32, tx))
            {
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                using var reader = await cmd.ExecuteReaderAsync();
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
            WHERE id = @id;", conn32, tx);

                    update.Parameters.AddWithValue("id", inv.Id);
                    update.Parameters.AddWithValue("desc", inv.Description);
                    update.Parameters.AddWithValue("qty", inv.Quantity);
                    update.Parameters.AddWithValue("rate", inv.Rate);
                    update.Parameters.AddWithValue("amt", inv.Amount);

                    await update.ExecuteNonQueryAsync();
                    Console.WriteLine($"🔄 Updated invoice: {inv.Description}");
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
                @id, @jobId, @desc, @qty, @rate, @amt, 2, TRUE, 0);", conn32, tx);

                    insert.Parameters.AddWithValue("id", newId);
                    insert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    insert.Parameters.AddWithValue("desc", inv.Description);
                    insert.Parameters.AddWithValue("qty", inv.Quantity);
                    insert.Parameters.AddWithValue("rate", inv.Rate);
                    insert.Parameters.AddWithValue("amt", inv.Amount);

                    await insert.ExecuteNonQueryAsync();
                    Console.WriteLine($"➕ Inserted invoice: {inv.Description}");
                }
            }
            Console.WriteLine("🔄 Syncing payments...");

            // 🗑️ Step 1: Delete payment if flagged
            if (job.DeletedPaymentId.HasValue)
            {
                using var delete = new NpgsqlCommand("DELETE FROM payment WHERE id = @id", conn32, tx);
                delete.Parameters.AddWithValue("id", job.DeletedPaymentId.Value);
                await delete.ExecuteNonQueryAsync();
                Console.WriteLine($"🗑️ Deleted payment by ID: {job.DeletedPaymentId.Value}");
                job.DeletedPaymentId = null; // Clear flag
            }

            // ✅ Step 2: Load existing payments from DB
            var existingPayments = new Dictionary<Guid, PaymentModel>();
            using (var cmd = new NpgsqlCommand(@"
    SELECT id, date, method, description, amount 
    FROM payment 
    WHERE jobnumber = @jobId;", conn32, tx))
            {
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                using var reader = await cmd.ExecuteReaderAsync();
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
            WHERE id = @id;", conn32, tx);

                    update.Parameters.AddWithValue("id", pay.Id);
                    update.Parameters.AddWithValue("dt", fullDateTime);
                    update.Parameters.AddWithValue("method", Convert.ToInt16(pay.Method ?? "0"));
                    update.Parameters.AddWithValue("desc", pay.Description ?? "");
                    update.Parameters.AddWithValue("amt", pay.Amount);

                    await update.ExecuteNonQueryAsync();
                    Console.WriteLine($"🔄 Updated payment: {pay.Description}");
                }
                else
                {
                    var newId = pay.Id != Guid.Empty ? pay.Id : Guid.NewGuid();

                    using var insert = new NpgsqlCommand(@"
            INSERT INTO payment (id, jobnumber, date, method, description, amount, changenum)
            VALUES (@id, @jobId, @dt, @method, @desc, @amt, 0);", conn32, tx);

                    insert.Parameters.AddWithValue("id", newId);
                    insert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    insert.Parameters.AddWithValue("dt", fullDateTime);
                    insert.Parameters.AddWithValue("method", Convert.ToInt16(pay.Method ?? "0"));
                    insert.Parameters.AddWithValue("desc", pay.Description ?? "");
                    insert.Parameters.AddWithValue("amt", pay.Amount);

                    await insert.ExecuteNonQueryAsync();
                    Console.WriteLine($"➕ Inserted payment: {pay.Description}");
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
                await delete.ExecuteNonQueryAsync();
                Console.WriteLine($"🗑️ Deleted attachment ID: {job.DeletedAttachmentId.Value}");
                job.DeletedAttachmentId = null;
            }

            // Step // Step 2: INSERT or UPDATE attachments
            foreach (var att in job.Attachments)
            {
                var blobId = att.BlobId != Guid.Empty ? att.BlobId : Guid.NewGuid();
                var attachmentId = att.Id != Guid.Empty ? att.Id : Guid.NewGuid();
                var changenum = new Random().Next(1000000, 9999999);

                // ✅ Insert into blobs (ON CONFLICT DO NOTHING)
                using (var insertBlob = new NpgsqlCommand(@"
        INSERT INTO blobs (id, blob)
        VALUES (@id, @blob)
        ON CONFLICT (id) DO NOTHING;", conn32, tx))
                {
                    insertBlob.Parameters.AddWithValue("id", blobId);
                    var blobParam = new NpgsqlParameter("blob", NpgsqlTypes.NpgsqlDbType.Bytea)
                    {
                        Value = att.FileData ?? Array.Empty<byte>()
                    };
                    insertBlob.Parameters.Add(blobParam);
                    await insertBlob.ExecuteNonQueryAsync();
                }

                // ✅ Insert or update blobmetadata (on changenum)
                using var insertMeta = new NpgsqlCommand(@"
        INSERT INTO blobmetadata (id, changenum, fileextension)
        VALUES (@id, @chg, @ext)
        ON CONFLICT (changenum) DO UPDATE SET fileextension = @ext;", conn32, tx);
                insertMeta.Parameters.AddWithValue("id", blobId);
                insertMeta.Parameters.AddWithValue("chg", changenum);
                insertMeta.Parameters.AddWithValue("ext", att.Format ?? "");
                await insertMeta.ExecuteNonQueryAsync();

                // ✅ Insert attachment (ignore if exists)
                using var insertAttachment = new NpgsqlCommand(@"
        INSERT INTO attachments (id, description, purpose, blobid, changenum)
        VALUES (@id, @desc, @purpose, @blobid, @chg)
        ON CONFLICT (id) DO NOTHING;", conn32, tx);
                insertAttachment.Parameters.AddWithValue("id", attachmentId);
                insertAttachment.Parameters.AddWithValue("desc", att.Description ?? "");
                insertAttachment.Parameters.AddWithValue("purpose", att.Purpose ?? "General");
                insertAttachment.Parameters.AddWithValue("blobid", blobId);
                insertAttachment.Parameters.AddWithValue("chg", changenum);
                await insertAttachment.ExecuteNonQueryAsync();

                // ✅ Insert papersattachmentscross (only if not already linked)
                await using var conn84 = new NpgsqlConnection(_connectionString);
                await conn84.OpenAsync();
                using var insertCross = new NpgsqlCommand(@"
        INSERT INTO papersattachmentscross (paperserialnum, attachmentid)
        VALUES (@paperId, @attachId)
        ON CONFLICT (paperserialnum, attachmentid) DO NOTHING;", conn84, tx);
                insertCross.Parameters.AddWithValue("paperId", int.Parse(job.JobId));
                insertCross.Parameters.AddWithValue("attachId", attachmentId);
                await insertCross.ExecuteNonQueryAsync();
            }



            // ✅ Clear deletion flag only AFTER all logic
            // ✅ Clear deletion flags AFTER all logic
            job.DeletedPaymentId = null;
            job.DeletedAttachmentId = null;

            Console.WriteLine("🟢 Committing transaction...");
            Console.WriteLine("✅ Job successfully saved.");
            await tx.CommitAsync();

        }
        catch (Exception ex)
        {
            tx.Rollback();
            Console.WriteLine($"🔥 ERROR in SaveJob(): {ex.Message}");
            throw;
        }
        // await using var conn84 = new NpgsqlConnection(_connectionString);
        //         await conn84.OpenAsync();
    }



    // Helper for nullable DateTime
    // ✅ Helper for nullable DateTime
    private static object ParseDateTimeOrNull(string dt)
    {
        return DateTime.TryParse(dt, out var parsed) ? parsed : DBNull.Value;
    }

    // ✅ Helper for converting string date + time to DateTime (safe for db)
    private static object CombineDateAndTime(string date, string time)
    {
        if (DateTime.TryParse($"{date} {time}", out var result))
            return result;
        return DBNull.Value;
    }

    // ✅ Convert string date + time to Unix timestamp (long)
    private static long ConvertToUnixTimestamp(string date, string time)
    {
        if (DateTime.TryParse($"{date} {time}", out var dt))
        {
            var offset = new DateTimeOffset(dt);
            return offset.ToUnixTimeSeconds(); // safe conversion
        }
        return 0;
    }

    // ✅ Async version: Generate next serial number for a table
    private static async Task<int> GenerateNewSerialAsync(string tableName, NpgsqlConnection conn, NpgsqlTransaction tx)
    {


        using var cmd = new NpgsqlCommand(
                    $"SELECT COALESCE(MAX(serialnum), 1000000) + 1 FROM {tableName};", conn, tx);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    // public async Task SearchJobAsync()
    // {
    //     // ... existing code ...
    //     var dbJob = await LoadDataAsync(SearchJobNumber, _cts.Token);

    //     Application.Current.Dispatcher.Invoke(() => {
    //         FilteredJobs.Clear();
    //         if (dbJob != null)
    //         {
    //             CurrentJob = dbJob;
    //             FilteredJobs.Add(dbJob);
    //             OnPropertyChanged(nameof(FilteredJobs));
    //         }
    //     }
    // );
    // ... rest of your code ...
}
