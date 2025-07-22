using System;
using Npgsql;
using CivilProcessERP.Models.Job;
using static CivilProcessERP.Models.Job.InvoiceModel;
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
    public System.Windows.Controls.ListView? AttachmentsListView { get; set; }
    private readonly string _connectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public List<AttachmentModel> Attachments { get; set; } = new List<AttachmentModel>();

    public bool IsPlaintiffsEdited { get; set; }
    public bool IsPlaintiffEdited { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;


    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task<Job?> GetJobById(string jobId)
    {
        Job job = new();
        // Defensive initialization for all collections
        job.InvoiceEntries = job.InvoiceEntries ?? new System.Collections.ObjectModel.ObservableCollection<InvoiceModel>();
        job.Payments = job.Payments ?? new System.Collections.ObjectModel.ObservableCollection<PaymentModel>();
        job.Attachments = job.Attachments ?? new System.Collections.ObjectModel.ObservableCollection<AttachmentModel>();
        job.Comments = job.Comments ?? new System.Collections.ObjectModel.ObservableCollection<CommentModel>();
        job.Attempts = job.Attempts ?? new System.Collections.ObjectModel.ObservableCollection<AttemptsModel>();
        job.ChangeHistory = job.ChangeHistory ?? new System.Collections.Generic.List<ChangeEntryModel>();
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            Console.WriteLine("[INFO] ✅ Connected to DB: " + conn.Database);

            // Step 1: Fetch from papers table
            await using (var cmd1 = new NpgsqlCommand("SELECT serialnum, caseserialnum FROM papers WHERE serialnum = @jobId", conn))
            {
                cmd1.Parameters.AddWithValue("jobId", long.Parse(jobId));
                await using (var reader1 = await cmd1.ExecuteReaderAsync())
                {
                    if (!await reader1.ReadAsync())
                    {
                        Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
                        return null;
                    }
                    job.JobId = reader1["serialnum"].ToString();
                    var caseSerial = reader1["caseserialnum"].ToString();
                    job.CaseNumber = caseSerial;
                }
            }
            Console.WriteLine("[DEBUG] After papers query, JobId: " + job.JobId);

            // Step 2: Fetch Court number (courtnum) from cases table
            string? courtNum = null;
            try {
                await using (var cmd2 = new NpgsqlCommand("SELECT courtnum FROM cases WHERE serialnum = @caseserialnum", conn))
                {
                    cmd2.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
                    await using (var reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        if (await reader2.ReadAsync())
                        {
                            courtNum = reader2["courtnum"]?.ToString();
                        }
                    }
                }
                Console.WriteLine($"[DEBUG] After cases.courtnum query, courtNum: {courtNum}");
            } catch (Exception ex) {
                Console.WriteLine($"[ERROR] Exception fetching cases.courtnum: {ex.Message}");
            }

            // Step 3: Fetch Court name from courts table based on courtnum
            if (!string.IsNullOrEmpty(courtNum))
            {
                try {
                    await using var conn2 = new NpgsqlConnection(_connectionString);
                    await conn2.OpenAsync();
                    await using (var cmd24 = new NpgsqlCommand("SELECT name FROM courts WHERE serialnum = @courtnum", conn2))
                    {
                        cmd24.Parameters.AddWithValue("courtnum", int.Parse(courtNum));
                        await using (var reader24 = await cmd24.ExecuteReaderAsync())
                        {
                            if (await reader24.ReadAsync())
                            {
                                job.Court = reader24["name"]?.ToString() ?? string.Empty;
                            }
                        }
                    }
                    Console.WriteLine($"[DEBUG] After courts.name query, Court: {job.Court}");
                } catch (Exception ex) {
                    Console.WriteLine($"[ERROR] Exception fetching courts.name: {ex.Message}");
                }
            }

            // Step 4: Fetch Defendant from cases table
            try {
                await using var conn3 = new NpgsqlConnection(_connectionString);
                await conn3.OpenAsync();
                await using (var cmd3 = new NpgsqlCommand("SELECT defend1 FROM cases WHERE serialnum = @caseserialnum", conn3))
                {
                    cmd3.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
                    await using (var reader3 = await cmd3.ExecuteReaderAsync())
                    {
                        if (await reader3.ReadAsync())
                        {
                            job.Defendant = reader3["defend1"]?.ToString() ?? string.Empty;
                        }
                    }
                }

                Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");

                await using var conn4 = new NpgsqlConnection(_connectionString);
                await conn4.OpenAsync();
                // Step 4: Fetch Zone
                await using (var cmd4 = new NpgsqlCommand("SELECT zone FROM papers WHERE serialnum = @jobId", conn4))
                {
                    cmd4.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using (var reader4 = await cmd4.ExecuteReaderAsync())
                    {
                        if (await reader4.ReadAsync())
                        {
                            job.Zone = reader4["zone"]?.ToString() ?? string.Empty;
                        }
                    }
                }

                await using var conn5 = new NpgsqlConnection(_connectionString);
                await conn5.OpenAsync();
                // Step 5: SQL Received Date
                await using (var cmd5 = new NpgsqlCommand("SELECT sqldatetimerecd FROM papers WHERE serialnum = @jobId", conn5))
                {
                    cmd5.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using (var reader5 = await cmd5.ExecuteReaderAsync())
                    {
                        if (await reader5.ReadAsync())
                        {
                            object sqlDateTimeRecdRaw = reader5["sqldatetimerecd"];
                            if (sqlDateTimeRecdRaw == DBNull.Value)
                                job.SqlDateTimeCreated = null;
                            else if (sqlDateTimeRecdRaw is DateTime dt)
                            {
                                if (dt == new DateTime(1972, 1, 1, 0, 0, 0))
                                    job.SqlDateTimeCreated = new DateTime(1972, 1, 1, 0, 0, 0);
                                else
                                    job.SqlDateTimeCreated = dt;
                            }
                            else if (sqlDateTimeRecdRaw is string s && DateTime.TryParse(s, out var parsed))
                                job.SqlDateTimeCreated = parsed;
                            else
                                job.SqlDateTimeCreated = null;
                        }
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
                        var servedDateRaw = reader6["sqldatetimeserved"];
                        if (servedDateRaw == DBNull.Value)
                            job.LastDayToServe = null;
                        else if (servedDateRaw is DateTime dt)
                            job.LastDayToServe = dt;
                        else if (servedDateRaw is string s && DateTime.TryParse(s, out var parsed))
                            job.LastDayToServe = parsed;
                        else
                            job.LastDayToServe = null;
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
                        object sqlExpireDateRaw = reader7["sqlexpiredate"];
                        if (sqlExpireDateRaw == DBNull.Value)
                            job.ExpirationDate = null;
                        else if (sqlExpireDateRaw is DateTime dt)
                        {
                            if (dt == new DateTime(1972, 1, 1, 0, 0, 0))
                                job.ExpirationDate = new DateTime(1972, 1, 1, 0, 0, 0);
                            else
                                job.ExpirationDate = dt;
                        }
                        else if (sqlExpireDateRaw is string s && DateTime.TryParse(s, out var parsed))
                            job.ExpirationDate = parsed;
                        else
                            job.ExpirationDate = null;
                    }
                }

                // Step 8: Get typeservice from papers
                string? typeServiceId = null;
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
                            job.ServiceType = reader9["servicename"]?.ToString() ?? string.Empty;
                        }
                    }
                }
                // Step 10: Get caseserialnum from papers
                string? caseSerialNum = null;
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
                            job.CaseNumber = reader11["casenum"]?.ToString() ?? string.Empty;  // ✅ correctly assign, never null
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
                        job.ClientReference = reader12["clientrefnum"]?.ToString() ?? string.Empty;  // ✅ assign ClientReference, never null
                    }
                }

                await using var conn13 = new NpgsqlConnection(_connectionString);
                await conn13.OpenAsync();
                //Step 12: Get Plaintiff Name from entity table using case number from papers
                if (!string.IsNullOrEmpty(caseSerialNum))
                {
                    await using (var connCase = new NpgsqlConnection(_connectionString))
                    {
                        await connCase.OpenAsync();
                        await using (var cmdCase = new NpgsqlCommand("SELECT pliannum FROM papers WHERE serialnum = @jobId", connCase))
                        {
                            cmdCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                            await using var readerCase = await cmdCase.ExecuteReaderAsync();
                            if (await readerCase.ReadAsync())
                            {
                                caseSerialNum = readerCase["pliannum"] != DBNull.Value ? readerCase["pliannum"].ToString() : null;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(caseSerialNum))
                    {
                        await using (var connEntity = new NpgsqlConnection(_connectionString))
                        {
                            await connEntity.OpenAsync();
                            await using (var cmdEntity = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\" FROM entity WHERE \"SerialNum\" = @pliannum", connEntity))
                            {
                                cmdEntity.Parameters.AddWithValue("pliannum", int.Parse(caseSerialNum));
                                await using var readerEntity = await cmdEntity.ExecuteReaderAsync();
                                if (await readerEntity.ReadAsync())
                                {
                                    var first = readerEntity["FirstName"]?.ToString();
                                    var last = readerEntity["LastName"]?.ToString();
                                    job.Plaintiff = $"{first} {last}".Trim();
                                }
                            }
                        }
                    }
                }

                // Step 13: Get attorneynum from papers table
                string? attorneySerial = null;
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
                string? clientSerial = null;
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
                string? serverCode = null;
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
                        job.TypeOfWrit = reader20["typewrit"]?.ToString() ?? string.Empty;
                    }
                }

                // Step 20 & 21: Get clientnum from papers → then status from entity
                string? clientnum = null;
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
                            job.ClientStatus = reader21["status"]?.ToString() ?? string.Empty;
                        }
                    }
                }
                // Step 22: Fetch courtdatecode and datetimeserved from papers
                string? courtDateCodeRaw = null;
                string? datetimeServedRaw = null;
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

                // --- COURT DATE ---
                Console.WriteLine($"[DEBUG] Raw value for courtdatecode: '{courtDateCodeRaw}', Type: {courtDateCodeRaw?.GetType()}");
                if (DateTime.TryParse(courtDateCodeRaw, out DateTime courtDateTime))
                {
                    job.CourtDateTime = courtDateTime;
                }
                else
                {
                    job.CourtDateTime = null;
                }

                // --- DATETIME SERVED ---
                Console.WriteLine($"[DEBUG] Raw value for datetimeserved: '{datetimeServedRaw}', Type: {datetimeServedRaw?.GetType()}");
                if (DateTime.TryParse(datetimeServedRaw, out DateTime servedDateTime))
                {
                    job.ServiceDateTime = servedDateTime;
                }
                else
                {
                    job.ServiceDateTime = null;
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

                        System.Windows.Application.Current.Dispatcher.Invoke(() => job.InvoiceEntries.Add(invoiceItem));
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

                    // Clear payments on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() => job.Payments.Clear());

                    while (await readerPayment.ReadAsync())
                    {
                        var dateTimeRaw = readerPayment["date"] != DBNull.Value
                            ? Convert.ToDateTime(readerPayment["date"])
                            : (DateTime?)null;

                        var payment = new PaymentModel
                        {
                            Id = readerPayment.GetGuid(0),
                            DateTime = dateTimeRaw,
                            Method = GetPaymentMethodString(readerPayment["method"]),
                            Description = readerPayment["description"]?.ToString() ?? string.Empty,
                            Amount = readerPayment["amount"] != DBNull.Value
                                ? Convert.ToDecimal(readerPayment["amount"])
                                : 0m
                        };

                        // Only add if not already present by Id
                        System.Windows.Application.Current.Dispatcher.Invoke(() => {
                            if (!job.Payments.Any(p => p.Id == payment.Id))
                                job.Payments.Add(payment);
                        });
                    }
                }

                // Shared timestamp offset
                // (removed duplicate declaration of timestampOffset)

                // Step: Load Comments
                await using var conn40 = new NpgsqlConnection(_connectionString);
                await conn40.OpenAsync();
                await using (var cmd90 = new NpgsqlCommand(@"SELECT 
    serialnum, seqnum, comment, datetime, source, isattempt, 
    printonaff, printonfs, reviewed 
    FROM comments 
    WHERE serialnum = @jobId AND isattempt = false", conn40))
                {
                    cmd90.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader90 = await cmd90.ExecuteReaderAsync();

                    while (await reader90.ReadAsync())
                    {
                        var comment = reader90["comment"]?.ToString() ?? string.Empty;
                        object datetimeRaw = reader90["datetime"];
                        DateTime? datetimeParsed = null;
                        if (datetimeRaw == DBNull.Value)
                            datetimeParsed = null;
                        else if (datetimeRaw is DateTime dt)
                            datetimeParsed = dt;
                        else if (datetimeRaw is long l && l > 946684800)
                            datetimeParsed = DateTimeOffset.FromUnixTimeSeconds(l).UtcDateTime;
                        else if (datetimeRaw is int i && i > 946684800)
                            datetimeParsed = DateTimeOffset.FromUnixTimeSeconds(i).UtcDateTime;
                        else if (datetimeRaw is string s)
                        {
                            if (DateTime.TryParse(s, out var dtStr))
                                datetimeParsed = dtStr;
                            else if (long.TryParse(s, out var l2) && l2 > 946684800)
                                datetimeParsed = DateTimeOffset.FromUnixTimeSeconds(l2).UtcDateTime;
                        }
                        int id = reader90["serialnum"] != DBNull.Value ? Convert.ToInt32(reader90["serialnum"]) : 0;
                        int seqNum = reader90["seqnum"] != DBNull.Value ? Convert.ToInt32(reader90["seqnum"]) : 0;
                        var source = reader90["source"]?.ToString() ?? "Unknown Source";
                        bool affChecked = reader90["printonaff"] != DBNull.Value && Convert.ToInt32(reader90["printonaff"]) > 0;
                        bool dsChecked = reader90["printonfs"] != DBNull.Value && Convert.ToInt32(reader90["printonfs"]) > 0;
                        bool attChecked = reader90["reviewed"] != DBNull.Value && Convert.ToBoolean(reader90["reviewed"]);

                        var commentModel = new CommentModel
                        {
                            SerialNum = id,
                            Seqnum = seqNum,
                            DateTime = datetimeParsed,
                            Body = comment,
                            Source = source,
                            Aff = affChecked,
                            FS = dsChecked,
                            Att = attChecked
                        };

                        System.Windows.Application.Current.Dispatcher.Invoke(() => job.Comments.Add(commentModel));
                    }
                }

                // Step: Load Attempts
                await using var conn25 = new NpgsqlConnection(_connectionString);
                await conn25.OpenAsync();
                await using (var cmd25 = new NpgsqlCommand(@"SELECT 
    serialnum, seqnum, comment, datetime, source, isattempt, 
    printonaff, printonfs, reviewed 
    FROM comments 
    WHERE serialnum = @jobId AND isattempt = true", conn25))
                {
                    cmd25.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader25 = await cmd25.ExecuteReaderAsync();

                    while (await reader25.ReadAsync())
                    {
                        var comment = reader25["comment"]?.ToString() ?? string.Empty;
                        object datetimeRawA = reader25["datetime"];
                        DateTime? datetimeParsedA = null;
                        if (datetimeRawA == DBNull.Value)
                            datetimeParsedA = null;
                        else if (datetimeRawA is DateTime dtA)
                            datetimeParsedA = dtA;
                        else if (datetimeRawA is long l && l > 946684800)
                            datetimeParsedA = DateTimeOffset.FromUnixTimeSeconds(l).UtcDateTime;
                        else if (datetimeRawA is int i && i > 946684800)
                            datetimeParsedA = DateTimeOffset.FromUnixTimeSeconds(i).UtcDateTime;
                        else if (datetimeRawA is string s)
                        {
                            if (DateTime.TryParse(s, out var dtStr))
                                datetimeParsedA = dtStr;
                            else if (long.TryParse(s, out var l2) && l2 > 946684800)
                                datetimeParsedA = DateTimeOffset.FromUnixTimeSeconds(l2).UtcDateTime;
                        }
                        int id = reader25["serialnum"] != DBNull.Value ? Convert.ToInt32(reader25["serialnum"]) : 0;
                        int seqNum = reader25["seqnum"] != DBNull.Value ? Convert.ToInt32(reader25["seqnum"]) : 0;
                        var source = reader25["source"]?.ToString() ?? "Unknown Source";
                        bool affChecked = reader25["printonaff"] != DBNull.Value && Convert.ToInt32(reader25["printonaff"]) > 0;
                        bool dsChecked = reader25["printonfs"] != DBNull.Value && Convert.ToInt32(reader25["printonfs"]) > 0;
                        bool attChecked = reader25["reviewed"] != DBNull.Value && Convert.ToBoolean(reader25["reviewed"]);

                        var attemptsModel = new AttemptsModel
                        {
                            SerialNum = id,
                            Seqnum = seqNum,
                            DateTime = datetimeParsedA,
                            Body = comment,
                            Source = source,
                            Aff = affChecked,
                            FS = dsChecked,
                            Att = attChecked
                        };

                        System.Windows.Application.Current.Dispatcher.Invoke(() => job.Attempts.Add(attemptsModel));
                    }
                }

                string address1 = "", address2 = "", state = "", city = "", zip = "";

                await using var conn41 = new NpgsqlConnection(_connectionString);
                await conn41.OpenAsync();
                // Step: Fetch Servee Address Details
                await using (var cmd26 = new NpgsqlCommand("SELECT address1, address2, state, city, zip FROM serveedetails WHERE serialnum = @jobId AND seqnum = 1", conn41))
                {
                    cmd26.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader26 = await cmd26.ExecuteReaderAsync();
                    if (await reader26.ReadAsync())
                    {
                        address1 = reader26["address1"]?.ToString() ?? "";
                        address2 = reader26["address2"]?.ToString() ?? "";
                        state = reader26["state"]?.ToString() ?? "";
                        city = reader26["city"]?.ToString() ?? "";
                        zip = reader26["zip"]?.ToString() ?? "";
                    }
                    job.AddressLine1 = address1;
job.AddressLine2 = address2;
job.City = city;
job.State = state;
job.Zip = zip;
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
                string? pliannum = null;
                await using var conn28 = new NpgsqlConnection(_connectionString);
                await conn28.OpenAsync();
                await using (var cmd28 = new NpgsqlCommand("SELECT serialnum FROM papers WHERE serialnum = @jobId", conn28))
                {
                    cmd28.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    await using var reader28 = await cmd28.ExecuteReaderAsync();
                    if (await reader28.ReadAsync())
                    {
                        pliannum = reader28["serialnum"]?.ToString();
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
                        var fullName = $"{firstName} {lastName}".Trim();
                        if (!string.IsNullOrWhiteSpace(fullName))
                            job.Plaintiffs = fullName;
                        // else keep the dudeservedlfm value
                    }
                }



                // Step: Fetch Attachments
                await using var conn31 = new NpgsqlConnection(_connectionString);
                await conn31.OpenAsync();
                await using (var cmd31 = new NpgsqlCommand(@"SELECT 
    a.id AS attachment_id, 
    a.blobid,
    a.description, 
    a.purpose, 
    bm.fileextension, 
    bm.id AS blobmetadata_id 
    FROM attachments a 
    JOIN papersattachmentscross pac ON pac.attachmentid = a.id 
    JOIN blobmetadata bm ON bm.id = a.blobid 
    WHERE pac.paperserialnum = @serialnum;", conn31))
                {
                    int serialnum = Convert.ToInt32(jobId);
                    cmd31.Parameters.AddWithValue("serialnum", serialnum);
                    await using var reader31 = await cmd31.ExecuteReaderAsync();

                    while (await reader31.ReadAsync())
                    {
                        job.Attachments.Add(new AttachmentModel
                        {
                            Id = reader31["attachment_id"] != DBNull.Value ? (Guid)reader31["attachment_id"] : Guid.Empty,
                            BlobId = reader31["blobid"] != DBNull.Value ? (Guid)reader31["blobid"] : Guid.Empty,
                            Purpose = reader31["purpose"]?.ToString() ?? string.Empty,
                            Description = reader31["description"]?.ToString() ?? string.Empty,
                            Format = reader31["fileextension"] != DBNull.Value ? reader31["fileextension"].ToString() ?? string.Empty : string.Empty,
                            BlobMetadataId = reader31["blobmetadata_id"] != DBNull.Value ? reader31["blobmetadata_id"].ToString() ?? string.Empty : string.Empty
                        });
                    }
                }

                // Finalize: Set address
                job.Address = $"{address1} {address2} {state} {city} {zip}".Trim();
                Console.WriteLine($"Concatenated Address: {job.Address}");

                Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");

                using var auditCmd = new NpgsqlCommand(@"
    INSERT INTO changehistory (jobid, action, username, details)
    VALUES (@jobid, @action, @username, @details);", conn);

                auditCmd.Parameters.AddWithValue("jobid", long.Parse(jobId));
                auditCmd.Parameters.AddWithValue("action", "SEARCH");
                auditCmd.Parameters.AddWithValue("username", SessionManager.CurrentUser?.LoginName ?? "Unknown");
                auditCmd.Parameters.AddWithValue("details", $"Job search performed for JobId: {jobId}");

                await auditCmd.ExecuteNonQueryAsync();

                var changeHistory = new List<ChangeEntryModel>();
                using var cmd = new NpgsqlCommand(@"
    SELECT action, username, details, changed_at
    FROM changehistory
    WHERE jobid = @jobid
    ORDER BY changed_at DESC;", conn);

                cmd.Parameters.AddWithValue("jobid", long.Parse(jobId));
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    changeHistory.Add(new ChangeEntryModel
                    {
                        Date = reader.GetDateTime(reader.GetOrdinal("changed_at")),
                        FieldName = reader.GetString(reader.GetOrdinal("action")),
                        OldValue = reader.GetString(reader.GetOrdinal("username")),
                        NewValue = reader.GetString(reader.GetOrdinal("details"))
                    });
                }
                job.ChangeHistory = changeHistory;

                // Fetch workflow status
                await using (var connWF = new NpgsqlConnection(_connectionString))
                {
                    await connWF.OpenAsync();
                    await using (var cmdWF = new NpgsqlCommand(
                        "SELECT workflowfcm, workflowsops, workflowiia FROM jobworkflowstatus WHERE jobid = @jobId", connWF))
                    {
                        cmdWF.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        await using var readerWF = await cmdWF.ExecuteReaderAsync();
                        if (await readerWF.ReadAsync())
                        {
                            job.WorkflowFCM = readerWF["workflowfcm"] != DBNull.Value && (bool)readerWF["workflowfcm"];
                            job.WorkflowSOPS = readerWF["workflowsops"] != DBNull.Value && (bool)readerWF["workflowsops"];
                            job.WorkflowIIA = readerWF["workflowiia"] != DBNull.Value && (bool)readerWF["workflowiia"];
                        }
                    }
                }

                Console.WriteLine($"[DEBUG] Searching for jobId: '{jobId}' (Type: {jobId.GetType()})");
                long parsedJobId;
                if (!long.TryParse(jobId, out parsedJobId))
                {
                    Console.WriteLine($"[DEBUG] Could not parse jobId '{jobId}' to long.");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Parsed jobId: {parsedJobId}");
                }

                Console.WriteLine("[DEBUG] Finished all DB queries in GetJobById");
           return job;
            }
             catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in GetJobById: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
        finally
        {
            Console.WriteLine("[DEBUG] Exiting GetJobById");
        }
        // Ensure all code paths return a value
        return null; // <-- Correctly close the try block here
    }
     catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in GetJobById: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
        finally
        {
            Console.WriteLine("[DEBUG] Exiting GetJobById");
        }
        // Ensure all code paths return a value
        return null;
    }

    public async Task SaveJob(Job job)
    {
        if (string.IsNullOrWhiteSpace(job.JobId) || !long.TryParse(job.JobId, out var parsedJobId))
        {
            Console.WriteLine($"[ERROR] Invalid or missing JobId: '{job.JobId}'");
            throw new ArgumentException($"Invalid or missing JobId: '{job.JobId}'");
        }
        RemoveDuplicatePayments(job);
        Console.WriteLine($"[DEBUG] Entered SaveJob for JobID: {job.JobId}");
        await using var conn32 = new NpgsqlConnection(_connectionString);
        await conn32.OpenAsync();
        await using var tx = await conn32.BeginTransactionAsync();
        try
        {
            Console.WriteLine("[DEBUG] Starting transaction in SaveJob");
            Console.WriteLine("🔄 Starting SaveJobAsync for JobID: " + job.JobId);

            // Try UPDATE
            Console.WriteLine("➡ Attempting to update papers table...");
            await using (var cmd = new NpgsqlCommand(@"
            UPDATE papers SET
                zone = @zone,
                sqldatetimerecd = @sqlDateCreated,
                sqldatetimeserved = @lastDayToServe,
                sqlexpiredate = @expirationDate,
                clientrefnum = @clientRef,
                courtdatecode = @courtDateCode,
                datetimeserved = @dateTimeServed
            WHERE serialnum = @jobId", conn32, tx))
            {
                cmd.Parameters.AddWithValue("zone", job.Zone ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("sqlDateCreated", ParseDateTimeOrNull(job.SqlDateTimeCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                cmd.Parameters.AddWithValue("lastDayToServe", ParseDateTimeOrNull(job.LastDayToServe?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                cmd.Parameters.AddWithValue("expirationDate", ParseDateTimeOrNull(job.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                cmd.Parameters.AddWithValue("clientRef", job.ClientReference ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("courtDateCode", ParseDateTimeOrNull(job.CourtDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                cmd.Parameters.AddWithValue("dateTimeServed", ParseDateTimeOrNull(job.ServiceDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected > 0)
                {
                    Console.WriteLine("✅ papers table updated.");
                    await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated papers table fields.");
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
                            if (!string.IsNullOrWhiteSpace(job.SqlDateTimeCreated?.ToString()))
                            {
                                fieldList.Add("sqldatetimerecd");
                                valueList.Add("@sqlDateCreated");
                                insertCmd.Parameters.AddWithValue("sqlDateCreated", ParseDateTimeOrNull(job.SqlDateTimeCreated?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                            }
                            if (!string.IsNullOrWhiteSpace(job.LastDayToServe?.ToString()))
                            {
                                fieldList.Add("sqldatetimeserved");
                                valueList.Add("@lastDayToServe");
                                insertCmd.Parameters.AddWithValue("lastDayToServe", ParseDateTimeOrNull(job.LastDayToServe?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                            }
                            if (!string.IsNullOrWhiteSpace(job.ExpirationDate?.ToString()))
                            {
                                fieldList.Add("sqlexpiredate");
                                valueList.Add("@expirationDate");
                                insertCmd.Parameters.AddWithValue("expirationDate", ParseDateTimeOrNull(job.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                            }
                            if (!string.IsNullOrWhiteSpace(job.ClientReference))
                            {
                                fieldList.Add("clientrefnum");
                                valueList.Add("@clientRef");
                                insertCmd.Parameters.AddWithValue("clientRef", job.ClientReference);
                            }
                            if (!string.IsNullOrWhiteSpace(job.CourtDateTime?.ToString()))
                            {
                                fieldList.Add("courtdatecode");
                                valueList.Add("@courtDateCode");
                                insertCmd.Parameters.AddWithValue("courtDateCode", ParseDateTimeOrNull(job.CourtDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
                            }
                            if (!string.IsNullOrWhiteSpace(job.ServiceDateTime?.ToString()))
                            {
                                fieldList.Add("datetimeserved");
                                valueList.Add("@dateTimeServed");
                                insertCmd.Parameters.AddWithValue("dateTimeServed", ParseDateTimeOrNull(job.ServiceDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""));
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
                                if (inserted > 0){
                                    Console.WriteLine("✅ Minimal papers row inserted.");
                                    await LogAuditAsync(conn32, tx, job.JobId, "INSERT", "Inserted minimal papers row.");}

                                else{
                                    Console.WriteLine("❌ INSERT into papers failed — check constraints or values.");}
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
                    if (affected > 0){
                        Console.WriteLine("✅ plongs.typewrit updated.");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated plongs.typewrit.");}
                    else{
                        Console.WriteLine("⚠️ No plongs row updated — serialnum may be missing.");}
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
                        int affectedRows = await cmdUpdateCourt.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine("✅ courts.name updated.");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated courts.name.");
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
                        int affectedRows = await cmdInsertCourt.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Inserted new court with serialnum = {newCourtSerial}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new court with serialnum = {newCourtSerial}.");
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
                        int affectedRows = await cmdUpdateCase.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine("✅ cases.courtnum linked to new court.");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Linked court to case.");
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
                        int affectedRows = await cmdUpdateDefendant.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ cases.defend1 updated to: {job.Defendant}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated cases.defend1.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No existing case found. Inserting new case and linking it...");

                    int newCaseSerial = await GenerateNewSerialAsync("cases", conn32, tx);

                    await using (var cmdInsertCase = new NpgsqlCommand(@"
            INSERT INTO cases (serialnum, defend1, changenum)
            VALUES (@serial, @defendant, @changenum);", conn32, tx))
                    {
                        cmdInsertCase.Parameters.AddWithValue("serial", newCaseSerial);
                        cmdInsertCase.Parameters.AddWithValue("defendant", job.Defendant);
                        cmdInsertCase.Parameters.AddWithValue("changenum", 0); // or another default value
                        int affectedRows = await cmdInsertCase.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Inserted new case with serialnum = {newCaseSerial}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new case with serialnum = {newCaseSerial}.");
                    }

                    await using (var cmdLinkCase = new NpgsqlCommand(@"
            UPDATE papers
            SET caseserialnum = @caseSerial
            WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLinkCase.Parameters.AddWithValue("caseSerial", newCaseSerial);
                        cmdLinkCase.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        int affectedRows = await cmdLinkCase.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Linked new case to papers.serialnum = {job.JobId}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Linked new case to papers.");
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
        WHERE serialnum = @jobId;", conn32, tx))
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
            WHERE serialnum = @serialnum AND seqnum = 1;", conn32, tx))
                    {
                        cmdUpdate.Parameters.AddWithValue("firstName", firstName);
                        cmdUpdate.Parameters.AddWithValue("lastName", lastName);
                        cmdUpdate.Parameters.AddWithValue("serialnum", plianNum.Value);
                        int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Plaintiffs (serveedetails) updated: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated serveedetails for plaintiffs.");
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
                        int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Inserted new serveedetails row with serialnum = {newPlianSerial}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new serveedetails row with serialnum = {newPlianSerial}.");
                    }
                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers
            SET pliannum = @plianSerial
            WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("plianSerial", newPlianSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Linked serveedetails.pliannum = {newPlianSerial} to papers.serialnum = {job.JobId}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Linked serveedetails to papers.");
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
                        int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Attorney updated: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated attorney in entity.");
                    }
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
                        int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Attorney entity inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", "Inserted and linked new attorney entity.");
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET attorneynum = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Attorney entity inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated attorney in entity.");
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
                        int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Client updated: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated client in entity.");
                    }
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
                        int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Client entity inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", "Inserted and linked new client entity.");
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET clientnum = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Client entity inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated client in entity.");
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
                        int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Process Server updated: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated process server in entity.");
                    }
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
                        int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", "Inserted and linked new process server entity.");
                    }

                    await using (var cmdLink = new NpgsqlCommand(@"
            UPDATE papers SET servercode = @serial WHERE serialnum = @jobId;", conn32, tx))
                    {
                        cmdLink.Parameters.AddWithValue("serial", newSerial);
                        cmdLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        int affectedRows = await cmdLink.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Process Server inserted and linked: {firstName} {lastName}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated process server in entity.");
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
        WHERE serialnum = @jobId AND seqnum = 1;", conn32, tx))
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
            WHERE serialnum = @serveeSerial AND seqnum = 1;", conn32, tx))
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
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated serveedetails address.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No serveedetails record found — inserting new record...");
                    int newServeeSerial = await GenerateNewSerialAsync("serveedetails", conn32, tx);

                    await using (var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO serveedetails (serialnum, seqnum, address1, address2, city, state, zip)
            VALUES (@serial, 1, @address1, @address2, @city, @state, @zip);", conn32, tx))
                    {
                        cmdInsert.Parameters.AddWithValue("serial", newServeeSerial);
                        cmdInsert.Parameters.AddWithValue("address1", address1);
                        cmdInsert.Parameters.AddWithValue("address2", address2);
                        cmdInsert.Parameters.AddWithValue("city", city);
                        cmdInsert.Parameters.AddWithValue("state", state);
                        cmdInsert.Parameters.AddWithValue("zip", zip);
                        int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"✅ Inserted new serveedetails record with serialnum = {newServeeSerial}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new serveedetails record with serialnum = {newServeeSerial}.");
                    }
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
                    int affectedRows = await cmdUpdate.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine($"✅ ServiceType updated to: {job.ServiceType}");
                    await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Updated typeservice.");
                }
                else
                {
                    int newTypeSerial = await GenerateNewSerialAsync("typeservice", conn32, tx);
                    await using var cmdInsert = new NpgsqlCommand(@"
            INSERT INTO typeservice (serialnumber, servicename)
            VALUES (@serial, @name);", conn32, tx);
                    cmdInsert.Parameters.AddWithValue("serial", newTypeSerial);
                    cmdInsert.Parameters.AddWithValue("name", job.ServiceType);
                    int affectedRows = await cmdInsert.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine($"✅ Inserted new ServiceType with ID = {newTypeSerial}");
                    await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new typeservice with ID = {newTypeSerial}.");


                    await using var cmdUpdateLink = new NpgsqlCommand(@"
            UPDATE papers
            SET typeservice = @serial
            WHERE serialnum = @jobId;", conn32, tx);
                    cmdUpdateLink.Parameters.AddWithValue("serial", newTypeSerial);
                    cmdUpdateLink.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                    //int affectedRows = await cmdUpdateLink.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine("✅ papers.typeservice linked to new service.");
                    await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", "Linked typeservice to papers.");
                }
                //         }
                //         //q -> comment list from memory
                //         var incoming = new Dictionary<int, CommentModel>();
                //         int seq = 1;
                //         foreach (var comment in job.Comments)
                //         {
                //             incoming[seq++] = comment;
                //         }
                //         Console.WriteLine("🔄 Refreshing comments in DB...");

                //         // Step 1: Get existing comment seqnums
                //         var existing = new Dictionary<int, string>();
                //         await using var connComments = new NpgsqlConnection(_connectionString);
                //         await connComments.OpenAsync();
                //         using (var cmd = new NpgsqlCommand(@"
                // SELECT seqnum, comment
                // FROM comments
                // WHERE serialnum = @jobId AND isattempt = false;", connComments, tx))
                //         {
                //             cmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                //             using var reader = await cmd.ExecuteReaderAsync();
                //             while (await reader.ReadAsync())
                //             {
                //                 int existingSeq = Convert.ToInt32(reader["seqnum"]);
                //                 string body = reader["comment"]?.ToString() ?? "";
                //                 existing[existingSeq] = body;
                //             }
                //         }

                //         foreach (var kvp in incoming)
                //         {
                //             var currentSeq = kvp.Key;
                //             var c = kvp.Value;
                //             long timestamp = ConvertToUnixTimestamp(c.Date, c.Time);

                //             if (existing.ContainsKey(currentSeq))
                //             {
                //                 using var update = new NpgsqlCommand(@"
                //         UPDATE comments
                //         SET comment = @comment,
                //             datetime = @datetime,
                //             source = @source,
                //             printonaff = @aff,
                //             printonfs = @fs,
                //             reviewed = @att
                //         WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

                //                 update.Parameters.AddWithValue("comment", c.Body ?? "");
                //                 update.Parameters.AddWithValue("datetime", timestamp);
                //                 update.Parameters.AddWithValue("source", c.Source ?? "UI");
                //                 update.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                //                 update.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                //                 update.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;
                //                 update.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                //                 update.Parameters.AddWithValue("seq", currentSeq);

                //                 int affectedRows = await update.ExecuteNonQueryAsync();
                //                 Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                //                 Console.WriteLine($"🔄 Updated comment seq = {currentSeq}");
                //                 await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", $"Updated comment seq = {currentSeq}.");
                //             }
                //             else
                //             {
                //                 using var insert = new NpgsqlCommand(@"
                //         INSERT INTO comments (
                //             serialnum, seqnum, changenum, comment, datetime, source,
                //             isattempt, printonaff, printonfs, reviewed)
                //         VALUES (
                //             @serialnum, @seqnum, 0, @comment, @datetime, @source,
                //             false, @aff, @fs, @att);", conn32, tx);

                //                 insert.Parameters.AddWithValue("serialnum", long.Parse(job.JobId));
                //                 insert.Parameters.AddWithValue("seqnum", currentSeq);
                //                 insert.Parameters.AddWithValue("comment", c.Body ?? "");
                //                 insert.Parameters.AddWithValue("datetime", timestamp);
                //                 insert.Parameters.AddWithValue("source", c.Source ?? "UI");
                //                 insert.Parameters.Add("aff", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.Aff ? (short)1 : (short)0;
                //                 insert.Parameters.Add("fs", NpgsqlTypes.NpgsqlDbType.Smallint).Value = c.FS ? (short)1 : (short)0;
                //                 insert.Parameters.Add("att", NpgsqlTypes.NpgsqlDbType.Boolean).Value = c.Att;

                //                 int affectedRows = await insert.ExecuteNonQueryAsync();
                //                 Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                //                 Console.WriteLine($"➕ Inserted new comment seq = {currentSeq}");
                //                 await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new comment seq = {currentSeq}.");
                //             }
                //         }

                //         // Step 4: Delete rows that were removed in memory
                //         var incomingSeqs = new HashSet<int>(incoming.Keys);
                //         foreach (var existingSeq in existing.Keys)
                //         {
                //             if (!incomingSeqs.Contains(existingSeq))
                //             {
                //                 using var deleteCmd = new NpgsqlCommand(@"
                //         DELETE FROM comments
                //         WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

                //                 deleteCmd.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                //                 deleteCmd.Parameters.AddWithValue("seq", existingSeq);
                //                 int affectedRows = await deleteCmd.ExecuteNonQueryAsync();
                //                 Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                //                 Console.WriteLine($"🗑️ Deleted comment seq = {existingSeq}");
                //                 await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted comment seq = {existingSeq}.");
                //             }
                //         }
                //         Console.WriteLine("🔄 Refreshing attempts in DB...");

                // Build dictionary from memory using actual seqnum
                var incoming = job.Comments
                    .Where(c => c.Seqnum > 0)
                    .ToDictionary(c => c.Seqnum);

                // Step 1: Fetch existing comments from DB
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
                        int seq = Convert.ToInt32(reader["seqnum"]);
                        string comment = reader["comment"]?.ToString() ?? "";
                        existing[seq] = comment;
                    }
                }

                // Step 2: Insert or Update
                foreach (var c in job.Comments)
                {
                    long timestamp = ConvertToUnixTimestamp(c.DateTime);

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
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

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
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", $"Updated comment seq = {c.Seqnum}.");
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
                false, @aff, @fs, @att);", conn32, tx);

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
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new comment seq = {c.Seqnum}.");
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
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = false;", conn32, tx);

                        delete.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        delete.Parameters.AddWithValue("seq", seq);

                        await delete.ExecuteNonQueryAsync();
                        Console.WriteLine($"🗑️ Deleted comment seq = {seq}");
                        await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted comment seq = {seq}.");
                    }
                }


// Step: Sync Attempts (exactly like Comments now)
Console.WriteLine("🔄 Syncing attempts in DB...");

                var incomingAttempts = new Dictionary<int, AttemptsModel>();
// Step: Assign new Seqnums to any new attempts
// Step 1: Load existing attempts from DB
var existingAttempts = new Dictionary<int, string>();
int maxSeq = 1000;

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

foreach (var kvp in incomingAttempts)
{
    var attemptSeq = kvp.Key;
    var a = kvp.Value;
    long ts = ConvertToUnixTimestamp(a.DateTime);

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

        int affectedRows = await update.ExecuteNonQueryAsync();
        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
        Console.WriteLine($"🔄 Updated attempt seq = {attemptSeq}");
        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", $"Updated attempt seq = {attemptSeq}.");
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

        int affectedRows = await insert.ExecuteNonQueryAsync();
        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
        Console.WriteLine($"➕ Inserted new attempt seq = {attemptSeq}");
        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted new attempt seq = {attemptSeq}.");
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
            WHERE serialnum = @jobId AND seqnum = @seq AND isattempt = true;", conn32, tx);

        delete.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
        delete.Parameters.AddWithValue("seq", existingSeq);
        int affectedRows = await delete.ExecuteNonQueryAsync();
        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
        Console.WriteLine($"🗑️ Deleted attempt seq = {existingSeq}");
        await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted attempt seq = {existingSeq}.");
    }
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

                        int affectedRows = await update.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🔄 Updated invoice: {inv.Description}");
                        await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", $"Updated invoice: {inv.Description}.");
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

                        int affectedRows = await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"➕ Inserted invoice: {inv.Description}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted invoice: {inv.Description}.");
                    }
                }
                Console.WriteLine("🔄 Syncing payments...");

                // 🗑️ Step 1: Delete payment if flagged
                if (job.DeletedPaymentId.HasValue)
                {
                    using var delete = new NpgsqlCommand("DELETE FROM payment WHERE id = @id", conn32, tx);
                    var deletedPaymentId = job.DeletedPaymentId.Value;
                    delete.Parameters.AddWithValue("id", deletedPaymentId);
                    int affectedRows = await delete.ExecuteNonQueryAsync();
                    Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                    Console.WriteLine($"🗑️ Deleted payment by ID: {deletedPaymentId}");
                    job.DeletedPaymentId = null; // Clear flag
                    await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted payment by ID: {deletedPaymentId}.");
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
                        object dateTimeRawPay = reader["date"];
                        DateTime? dateTimeParsedPay = dateTimeRawPay != DBNull.Value ? (DateTime?)dateTimeRawPay : null;
                        if (dateTimeRawPay != DBNull.Value)
                        {
                            if (dateTimeRawPay is DateTime dtPay)
                            {
                                dateTimeParsedPay = dtPay;
                            }
                            else if (long.TryParse(dateTimeRawPay.ToString(), out long legacyTimestampPay) && legacyTimestampPay > 946684800)
                            {
                                var correctedTimestampPay = legacyTimestampPay + 1225178692;
                                dateTimeParsedPay = DateTimeOffset.FromUnixTimeSeconds(correctedTimestampPay).UtcDateTime;
                            }
                        }
                        var payment = new PaymentModel
                        {
                            Id = id,
                            DateTime = dateTimeParsedPay,
                            Method = GetPaymentMethodString(reader["method"]),
                            Description = reader["description"]?.ToString() ?? "",
                            Amount = Convert.ToDecimal(reader["amount"])
                        };
                        existingPayments[id] = payment;
                        System.Windows.Application.Current.Dispatcher.Invoke(() => job.Payments.Add(payment));
                    }
                }

                // Step 3: Insert or Update only as needed
                foreach (var pay in job.Payments.ToList())
                {
                    // Only skip if payment is invalid
                    if (pay.Amount <= 0 || string.IsNullOrWhiteSpace(pay.Description))
                        continue;
                    if (job.DeletedPaymentId.HasValue && pay.Id == job.DeletedPaymentId.Value)
                        continue; // Skip deleted item

                    if (pay.Id == Guid.Empty)
                    {
                        // New payment: assign new Guid and insert
                        pay.Id = Guid.NewGuid();
                        using var insert = new NpgsqlCommand(@"
    INSERT INTO payment (id, jobnumber, date, method, description, amount, changenum)
    VALUES (@id, @jobId, @dt, @method, @desc, @amt, 0);", conn32, tx);

                        insert.Parameters.AddWithValue("id", pay.Id);
                        insert.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
                        insert.Parameters.AddWithValue("dt", pay.DateTime ?? DateTime.Now);
                        insert.Parameters.AddWithValue("method", GetPaymentMethodCode(pay.Method));
                        insert.Parameters.AddWithValue("desc", pay.Description ?? "");
                        insert.Parameters.AddWithValue("amt", pay.Amount);

                        int affectedRows = await insert.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"➕ Inserted payment: {pay.Description}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted payment: {pay.Description}.");
                        continue;
                    }

                    if (existingPayments.TryGetValue(pay.Id, out var existingPayment))
                    {
                        // Only update if something changed
                        if (pay.DateTime != existingPayment.DateTime || pay.Method != existingPayment.Method || pay.Description != existingPayment.Description || pay.Amount != existingPayment.Amount)
                        {
                            using var update = new NpgsqlCommand(@"
    UPDATE payment
    SET date = @dt, method = @method, description = @desc, amount = @amt
    WHERE id = @id;", conn32, tx);

                            update.Parameters.AddWithValue("id", pay.Id);
                            update.Parameters.AddWithValue("dt", pay.DateTime ?? DateTime.Now);
                            update.Parameters.AddWithValue("method", GetPaymentMethodCode(pay.Method));
                            update.Parameters.AddWithValue("desc", pay.Description ?? "");
                            update.Parameters.AddWithValue("amt", pay.Amount);

                            int affectedRows = await update.ExecuteNonQueryAsync();
                            if (affectedRows == 0)
                                Console.WriteLine($"[WARN] Update for payment {pay.Id} affected 0 rows. This may indicate a mismatch or bug.");
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"🔄 Updated payment: {pay.Description}");
                            await LogAuditAsync(conn32, tx, job.JobId, "UPDATE", $"Updated payment: {pay.Description}.");
                        }
                        // else: no change, do nothing
                    }
                    else if (pay.Id != Guid.Empty && !existingPayments.ContainsKey(pay.Id))
                    {
                        // Do not insert if the payment Id is not empty but not found in DB (prevents accidental re-inserts)
                        Console.WriteLine($"[WARN] Payment with Id {pay.Id} not found in DB, skipping insert to avoid duplicate.");
                        continue;
                    }
                }
                // Remove duplicates after all modifications
                RemoveDuplicatePayments(job);

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
                    var deletedAttachmentId = job.DeletedAttachmentId.Value;
                    job.DeletedAttachmentId = null;
                    await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted attachment ID: {deletedAttachmentId}.");
                }

                // Step // Step 2: INSERT or UPDATE attachments
                foreach (var att in job.Attachments)
                {
                    if (att.Status == "New")
                    {
                        Console.WriteLine($"[DEBUG] Inserting new attachment: ID={att.Id}, Filename={att.Filename}, Description={att.Description}, Format={att.Format}, Purpose={att.Purpose}");

                        using var insertAttachment = new NpgsqlCommand(@"
    INSERT INTO attachments (id, blobid, description, purpose, filename, changenum)
    VALUES (@id, @blobid, @desc, @purpose, @filename, @changenum);", conn32, tx);

                        insertAttachment.Parameters.AddWithValue("id", att.Id);
                        insertAttachment.Parameters.AddWithValue("blobid", att.BlobId);
                        insertAttachment.Parameters.AddWithValue("desc", att.Description ?? "");
                        insertAttachment.Parameters.AddWithValue("purpose", int.TryParse(att.Purpose, out var p) ? p : 1);
                        insertAttachment.Parameters.AddWithValue("filename", att.Filename ?? "");
                        insertAttachment.Parameters.AddWithValue("changenum", 0); // or generate a value if needed

                        int affectedRows = await insertAttachment.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Inserted attachment: ID={att.Id}, Rows affected: {affectedRows}");
                        await LogAuditAsync(conn32, tx, job.JobId, "INSERT", $"Inserted attachment: {att.Description}.");

                        // Optionally insert into blobmetadata, blobs, and papersattachmentscross as needed
                        att.Status = "Synced";

                        using var insertCross = new NpgsqlCommand(@"
    INSERT INTO papersattachmentscross (id, paperserialnum, attachmentid, changenum)
    VALUES (@id, @paperserialnum, @attachmentid, @changenum);", conn32, tx);

                        insertCross.Parameters.AddWithValue("id", Guid.NewGuid());
                        insertCross.Parameters.AddWithValue("paperserialnum", long.Parse(job.JobId));
                        insertCross.Parameters.AddWithValue("attachmentid", att.Id);
                        insertCross.Parameters.AddWithValue("changenum", 0); // or a real value if needed

                        await insertCross.ExecuteNonQueryAsync();

                        using var insertBlobMeta = new NpgsqlCommand(@"
    INSERT INTO blobmetadata (id, changenum, fileextension)
    VALUES (@id, @changenum, @fileextension)
    ON CONFLICT (id) DO NOTHING;", conn32, tx);

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
            WHERE id = @id;", conn32, tx);
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
            WHERE id = @blobid;", conn32, tx);
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
                    string? caseSerialNum = null;
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

                        await using (var cmd = new NpgsqlCommand(@"
                        UPDATE entity
                        SET ""FirstName"" = @firstName, ""LastName"" = @lastName
                        WHERE ""SerialNum"" = @caseSerialNum;", conn32, tx))
                        {
                            cmd.Parameters.AddWithValue("firstName", firstName);
                            cmd.Parameters.AddWithValue("lastName", lastName);
                            cmd.Parameters.AddWithValue("caseSerialNum", int.Parse(caseSerialNum));
                            int affectedRows = await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                            Console.WriteLine($"✅ entity updated for plaintiff: {firstName} {lastName} (SerialNum: {serialNum})");
                        }
                        Console.WriteLine($"✅ entity updated for plaintiff: {firstName} {lastName} (SerialNum: {serialNum})");
                    }
                }

                Console.WriteLine("🟢 Committing transaction...");
                Console.WriteLine("✅ Job successfully saved.");
                await tx.CommitAsync();
                Console.WriteLine("[DEBUG] Transaction committed in SaveJob");
                await LogAuditAsync(conn32, tx, job.JobId, "COMMIT", "Committed transaction for SaveJob.");

                // Upsert workflow status
                await using (var cmdWF = new NpgsqlCommand(@"
    INSERT INTO jobworkflowstatus (jobid, workflowfcm, workflowsops, workflowiia)
    VALUES (@jobid, @fcm, @sops, @iia)
    ON CONFLICT (jobid) DO UPDATE SET
        workflowfcm = EXCLUDED.workflowfcm,
        workflowsops = EXCLUDED.workflowsops,
        workflowiia = EXCLUDED.workflowiia;
", conn32, tx))
                {
                    cmdWF.Parameters.AddWithValue("jobid", long.Parse(job.JobId));
                    cmdWF.Parameters.AddWithValue("fcm", job.WorkflowFCM);
                    cmdWF.Parameters.AddWithValue("sops", job.WorkflowSOPS);
                    cmdWF.Parameters.AddWithValue("iia", job.WorkflowIIA);
                    await cmdWF.ExecuteNonQueryAsync();
                }

                // Debug: Log all payment Ids and details before saving
                Console.WriteLine("[DEBUG] Payments to be saved:");
                foreach (var pay in job.Payments)
                {
                    Console.WriteLine($"  Id: {pay.Id}, Amount: {pay.Amount}, Desc: {pay.Description}, Method: {pay.Method}, Date: {pay.DateTime}");
                }
                var idGroups = job.Payments.GroupBy(p => p.Id).Where(g => g.Count() > 1).ToList();
                if (idGroups.Any())
                {
                    Console.WriteLine("[WARN] Duplicate payment Ids detected in Job.Payments:");
                    foreach (var group in idGroups)
                    {
                        Console.WriteLine($"  Id: {group.Key}, Count: {group.Count()}");
                    }
                }

                // --- Invoice Deletion Logic ---
                var memoryInvoiceIds = new HashSet<Guid>(job.InvoiceEntries.Select(inv => inv.Id));
                foreach (var dbInvoiceId in existingInvoices.Keys)
                {
                    if (!memoryInvoiceIds.Contains(dbInvoiceId))
                    {
                        Console.WriteLine($"[DEBUG] Deleting invoice from DB: {dbInvoiceId}");
                        using var delete = new NpgsqlCommand("DELETE FROM joblineitem WHERE id = @id", conn32, tx);
                        delete.Parameters.AddWithValue("id", dbInvoiceId);
                        int affectedRows = await delete.ExecuteNonQueryAsync();
                        Console.WriteLine($"[DEBUG] Rows affected: {affectedRows}");
                        Console.WriteLine($"🗑️ Deleted invoice by ID: {dbInvoiceId}");
                        await LogAuditAsync(conn32, tx, job.JobId, "DELETE", $"Deleted invoice by ID: {dbInvoiceId}.");
                        
                    }
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Exception in SaveJob: {ex.Message}\n{ex.StackTrace}");
            tx.Rollback();
            throw;
        }
        finally
        {
            Console.WriteLine("[DEBUG] Exiting SaveJob");
        }
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
    private static long ConvertToUnixTimestamp(DateTime? dateTime)
    {
        if (dateTime.HasValue)
        {
            var offset = new DateTimeOffset(dateTime.Value);
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

    public async Task<int> GetJobCountByCaseNumberAsync(string caseNumber)
    {
        if (string.IsNullOrWhiteSpace(caseNumber)) return 0;
        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using (var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM papers p
            JOIN cases c ON p.caseserialnum = c.serialnum
            WHERE c.casenum = @caseNumber", conn))
            {
                cmd.Parameters.AddWithValue("caseNumber", caseNumber.Trim());
                var result = await cmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);
                Console.WriteLine($"[INFO] Found {count} jobs with case number '{caseNumber.Trim()}'");
                return Convert.ToInt32(result);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to get job count for case number {caseNumber}: {ex.Message}");
            return 0;
        }
    }

    private void RemoveDuplicatePayments(Job job)
    {
        if (job?.Payments == null) return;
        var uniquePayments = job.Payments
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();
        System.Windows.Application.Current.Dispatcher.Invoke(() => {
            job.Payments.Clear();
            foreach (var pay in uniquePayments)
                job.Payments.Add(pay);
        });
    }

    // Helper to map payment method string to int code for DB
    private int GetPaymentMethodCode(string? method)
    {
        return method?.ToLower() switch
        {
            "cash" => 0,
            "check" => 1,
            "credit" => 2,
            "card" => 2, // Treat 'Card' as 'Credit'
            "debit" => 3,
            // Add more mappings as needed
            _ => 0 // Default to Cash
        };
    }

    // Add this helper to map int code to payment method string
    private string GetPaymentMethodString(object? dbValue)
    {
        if (dbValue == null || dbValue == DBNull.Value) return "Cash";
        int code = 0;
        if (dbValue is int i) code = i;
        else if (dbValue is short s) code = s;
        else int.TryParse(dbValue.ToString(), out code);
        return code switch
        {
            0 => "Cash",
            1 => "Check",
            2 => "Card", // Prefer 'Card' for UI consistency
            3 => "Debit",
            _ => "Cash"
        };
    }
}
