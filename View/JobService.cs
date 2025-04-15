using System;
using Npgsql;
using CivilProcessERP.Models.Job;
using static CivilProcessERP.Models.Job.InvoiceModel;


// Ensure JobLineItem is defined in the namespace CivilProcessERP.Models.Job

using System.ComponentModel;

public class JobService : INotifyPropertyChanged
{
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public List<InvoiceModel> InvoiceEntries { get; set; } = new List<InvoiceModel>();

    public event PropertyChangedEventHandler PropertyChanged;

    public decimal TotalInvoiceAmounts
    {
        get
        {
            return InvoiceEntries?.Sum(x => x.Amount) ?? 0;
        }
    }

    public void RecalculateTotals()
{
    OnPropertyChanged(nameof(TotalInvoiceAmounts));
    //OnPropertyChanged(nameof(TotalPaymentsAmount));
}

public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;


    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

   public Job GetJobById(string jobId)
{
    Job job = new();

    try
    {
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        Console.WriteLine("[INFO] ✅ Connected to DB: " + conn.Database);

        // Step 1: Fetch from papers table
        using (var cmd1 = new NpgsqlCommand("SELECT serialnum, caseserialnum FROM papers WHERE serialnum = @jobId", conn))
        {
            cmd1.Parameters.AddWithValue("jobId", long.Parse(jobId));
            using var reader1 = cmd1.ExecuteReader();

            if (!reader1.Read())
            {
                Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
                return null;
            }

            job.JobId = reader1["serialnum"].ToString();
            var caseSerial = reader1["caseserialnum"].ToString();
            reader1.Close(); // ✅ important
            job.CaseNumber = caseSerial;
        }

        // Step 2: Fetch Court from cases table
        using (var cmd2 = new NpgsqlCommand("SELECT typecourt FROM cases WHERE serialnum = @caseserialnum", conn))
        {
            cmd2.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
            using var reader2 = cmd2.ExecuteReader();
            if (reader2.Read())
            {
                job.Court = reader2["typecourt"]?.ToString();
            }
            reader2.Close(); // ✅ make sure this is closed
        }

        // Step 3: Fetch Defendant from cases table
        using (var cmd3 = new NpgsqlCommand("SELECT defend1 FROM cases WHERE serialnum = @caseserialnum", conn))
        {
            cmd3.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
            using var reader3 = cmd3.ExecuteReader();
            if (reader3.Read())
            {
                job.Defendant = reader3["defend1"]?.ToString();
            }
            reader3.Close();
        }
// Step 4: Fetch Zone
using (var cmd4 = new NpgsqlCommand("SELECT zone FROM papers WHERE serialnum = @jobId", conn))
{
    cmd4.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader4 = cmd4.ExecuteReader();
    if (reader4.Read())
    {
        job.Zone = reader4["zone"]?.ToString();
    }
    reader4.Close();
}

// Step 5: SQL Received Date
using (var cmd5 = new NpgsqlCommand("SELECT sqldatetimerecd FROM papers WHERE serialnum = @jobId", conn))
{
    cmd5.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader5 = cmd5.ExecuteReader();
    if (reader5.Read())
    {
        job.SqlDateTimeCreated = reader5["sqldatetimerecd"]?.ToString();
    }
    reader5.Close();
}

// Step 6: SQL Served Date
using (var cmd6 = new NpgsqlCommand("SELECT sqldatetimeserved FROM papers WHERE serialnum = @jobId", conn))
{
    cmd6.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader6 = cmd6.ExecuteReader();
    if (reader6.Read())
    {
        job.LastDayToServe = reader6["sqldatetimeserved"]?.ToString();
    }
    reader6.Close();
}

// Step 7: Expiration Date
using (var cmd7 = new NpgsqlCommand("SELECT sqlexpiredate FROM papers WHERE serialnum = @jobId", conn))
{
    cmd7.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader7 = cmd7.ExecuteReader();
    if (reader7.Read())
    {
        job.ExpirationDate = reader7["sqlexpiredate"]?.ToString();
    }
    reader7.Close();
}

// Step 8: Get typeservice from papers
string typeServiceId = null;
using (var cmd8 = new NpgsqlCommand("SELECT typeservice FROM papers WHERE serialnum = @jobId", conn))
{
    cmd8.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader8 = cmd8.ExecuteReader();
    if (reader8.Read())
    {
        typeServiceId = reader8["typeservice"]?.ToString();
    }
    reader8.Close();
}

// Step 9: Get servicename from typeservice table
if (!string.IsNullOrEmpty(typeServiceId))
{
    using (var cmd9 = new NpgsqlCommand("SELECT servicename FROM typeservice WHERE serialnumber = @typeservice", conn))
    {
        cmd9.Parameters.AddWithValue("typeservice", int.Parse(typeServiceId));
        using var reader9 = cmd9.ExecuteReader();
        if (reader9.Read())
        {
            job.ServiceType = reader9["servicename"]?.ToString(); // ✅ assign to ServiceType
        }
        reader9.Close();
    }
}
// Step 10: Get caseserialnum from papers
string caseSerialNum = null;
using (var cmd10 = new NpgsqlCommand("SELECT caseserialnum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd10.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader10 = cmd10.ExecuteReader();
    if (reader10.Read())
    {
        caseSerialNum = reader10["caseserialnum"]?.ToString();
    }
    reader10.Close();
}

// Step 11: Get casenum from cases table
if (!string.IsNullOrEmpty(caseSerialNum))
{
    using (var cmd11 = new NpgsqlCommand("SELECT casenum FROM cases WHERE serialnum = @caseserialnum", conn))
    {
        cmd11.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerialNum));
        using var reader11 = cmd11.ExecuteReader();
        if (reader11.Read())
        {
            job.CaseNumber = reader11["casenum"]?.ToString();  // ✅ correctly assign to CaseNumber
        }
        reader11.Close();
    }
}

using (var cmd12 = new NpgsqlCommand("SELECT clientrefnum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd12.Parameters.AddWithValue("jobId", long.Parse(job.JobId));// safely bind as string
    using var reader12 = cmd12.ExecuteReader();
    if (reader12.Read())
    {
        job.ClientReference = reader12["clientrefnum"]?.ToString();  // ✅ fixed column name
    }
    reader12.Close();
}

// Step 12: Get Plaintiff Name from entity table using serialnumber = case number
if (!string.IsNullOrEmpty(caseSerialNum))
{
    using (var cmd13 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\"  FROM entity WHERE \"SerialNum\" = @caseSerialNum", conn))
    {
        cmd13.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerialNum));
        using var reader13 = cmd13.ExecuteReader();
        if (reader13.Read())
        {
            var first = reader13["FirstName"]?.ToString();
            var last = reader13["LastName"]?.ToString();
            job.Plaintiff = $"{first} {last}".Trim();  // ✅ assign full name
        }
        reader13.Close();
    }
}


// Step 13: Get attorneynum from papers table
string attorneySerial = null;
using (var cmd13 = new NpgsqlCommand("SELECT attorneynum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd13.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader13 = cmd13.ExecuteReader();
    if (reader13.Read())
    {
        attorneySerial = reader13["attorneynum"]?.ToString();
    }
    reader13.Close();
}

// Step 14: Get Attorney's name from entity table
if (!string.IsNullOrEmpty(attorneySerial))
{
    using (var cmd14 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\"  FROM entity WHERE \"SerialNum\" = @attorneySerial", conn))
    {
        cmd14.Parameters.AddWithValue("attorneySerial", int.Parse(attorneySerial));
        using var reader14 = cmd14.ExecuteReader();
        if (reader14.Read())
        {
            var first = reader14["FirstName"]?.ToString();
            var last = reader14["LastName"]?.ToString();
            job.Attorney = $"{first} {last}".Trim();  // ✅ assign full attorney name
        }
        reader14.Close();
    }
}


// Step 15: Get clientnum from papers table
string clientSerial = null;
using (var cmd15 = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd15.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader15 = cmd15.ExecuteReader();
    if (reader15.Read())
    {
        clientSerial = reader15["clientnum"]?.ToString();
    }
    reader15.Close();
}

// Step 16: Get Client's name from entity table
if (!string.IsNullOrEmpty(clientSerial))
{
    using (var cmd16 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\"  FROM entity WHERE \"SerialNum\" = @clientSerial", conn))
    {
        cmd16.Parameters.AddWithValue("clientSerial", int.Parse(clientSerial));
        using var reader16 = cmd16.ExecuteReader();
        if (reader16.Read())
        {
            var first = reader16["FirstName"]?.ToString();
            var last = reader16["LastName"]?.ToString();
            job.Client = $"{first} {last}".Trim();  // ✅ assign full client name
        }
        reader16.Close();
    }
}

// // Step 17: Get Client Status from entity using clientnum
// if (!string.IsNullOrEmpty(clientSerial))
// {
//     using (var cmd17 = new NpgsqlCommand("SELECT \"status\" FROM entity WHERE \"SerialNum\" = @clientSerial", conn))
//     {
//         cmd17.Parameters.AddWithValue("clientSerial", int.Parse(clientSerial));
//         using var reader17 = cmd17.ExecuteReader();
//         if (reader17.Read())
//         {
//             job.ClientStatus = reader17["status"]?.ToString();  // ✅ assign status
//         }
//         reader17.Close();
//     }
// }

// Step 1: Get servercode from papers
string serverCode = null;
using (var cmd18 = new NpgsqlCommand("SELECT servercode FROM papers WHERE serialnum = @jobId", conn))
{
    cmd18.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader18 = cmd18.ExecuteReader();
    if (reader18.Read())
    {
        serverCode = reader18["servercode"]?.ToString();
    }
    reader18.Close();
}

// Step 2: Get Process Server name from entity table
if (!string.IsNullOrEmpty(serverCode))
{
    using (var cmd19 = new NpgsqlCommand("SELECT \"FirstName\", \"LastName\"  FROM entity WHERE \"SerialNum\" = @servercode", conn))
    {
        cmd19.Parameters.AddWithValue("servercode", int.Parse(serverCode));
        using var reader19 = cmd19.ExecuteReader();
        if (reader19.Read())
        {
            var first = reader19["FirstName"]?.ToString();
            var last = reader19["LastName"]?.ToString();
            job.ProcessServer = $"{first} {last}".Trim(); // full name
        }
        reader19.Close();
    }
}

using (var cmd20 = new NpgsqlCommand("SELECT typewrit FROM plongs WHERE serialnum = @jobId", conn))
{
    cmd20.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader20 = cmd20.ExecuteReader();
    if (reader20.Read())
    {
        job.TypeOfWrit = reader20["typewrit"]?.ToString();
    }
    reader20.Close();
}

if (!string.IsNullOrEmpty(caseSerialNum))
{
    using (var cmd21 = new NpgsqlCommand("SELECT \"status\" FROM entity WHERE \"SerialNum\" = @caseSerialNum", conn))
    {
        cmd21.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerialNum));
        using var reader21 = cmd21.ExecuteReader();
        if (reader21.Read())
        {
            job.ClientStatus = reader21["status"]?.ToString();  // ✅ assign full name
        }
        reader21.Close();
    }
}
string courtDateCodeRaw = null;
string datetimeServedRaw = null;

using (var cmd22 = new NpgsqlCommand("SELECT courtdatecode, datetimeserved FROM papers WHERE serialnum = @jobId", conn))
{
    cmd22.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader22 = cmd22.ExecuteReader();
    if (reader22.Read())
    {
        courtDateCodeRaw = reader22["courtdatecode"]?.ToString();
        datetimeServedRaw = reader22["datetimeserved"]?.ToString();
    }
    reader22.Close();
}

// --- COURT DATE ---
if (long.TryParse(courtDateCodeRaw, out long courtTimestamp) && courtTimestamp != 0)
{
    var courtDateTime = DateTimeOffset.FromUnixTimeSeconds(courtTimestamp).UtcDateTime;
    job.Date = courtDateTime.ToString("yyyy-MM-dd");
    job.Time = courtDateTime.ToString("HH:mm:ss");
}
else
{
    job.Date = "N/A";
    job.Time = "N/A";
}

// --- DATETIME SERVED ---
if (long.TryParse(datetimeServedRaw, out long servedTimestamp) && servedTimestamp != 0)
{
    var servedDateTime = DateTimeOffset.FromUnixTimeSeconds(servedTimestamp).UtcDateTime;
    job.ServiceDate = servedDateTime.ToString("yyyy-MM-dd");
    job.ServiceTime = servedDateTime.ToString("HH:mm:ss");
}
else
{
    job.ServiceDate = "N/A";
    job.ServiceTime = "N/A";
}

 // Step 1: Get joblineitem details from the joblineitem table
string description = null;
int quantity = 0;
decimal rate = 0m;
decimal amount = 0m;
using (var cmd23 = new NpgsqlCommand("SELECT description, quantity::decimal, rate::decimal, amount::decimal FROM joblineitem WHERE jobnumber = @jobId", conn))
{
    cmd23.Parameters.AddWithValue("jobId", long.Parse(job.JobId)); // Use jobId from papers table

    using var reader23 = cmd23.ExecuteReader();
    while (reader23.Read())
    {
        description = reader23["description"]?.ToString() ?? "No Description";
        quantity = reader23["quantity"] != DBNull.Value ? (int)Convert.ToDecimal(reader23["quantity"]) : 0;
        rate = reader23["rate"] != DBNull.Value ? Convert.ToDecimal(reader23["rate"]) : 0m;
        amount = reader23["amount"] != DBNull.Value ? Convert.ToDecimal(reader23["amount"]) : 0m;

        // If quantity, rate, or amount is zero, use a default message
        if (quantity == 0m || rate == 0m || amount == 0m)
        {
            Console.WriteLine($"[INFO] Zero data found for description: {description}. Consider verifying database.");
        }

        var invoiceItem = new InvoiceModel
        {
            Description = description,
            Quantity = (int)quantity, // Cast to int if needed
            Rate = rate,
            Amount = amount
        };

        job.InvoiceEntries.Add(invoiceItem);
        // Removed JobDetailsView.RecalculateTotals(); as it is not defined in the current context
        // Ensure to implement recalculation logic elsewhere if needed
        RecalculateTotals();
        

    }

    reader23.Close();

    Console.WriteLine($"Description: {description}, Quantity: {quantity}, Rate: {rate}, Amount: {amount}");
}


// Now you have the values for description, quantity, rate, amount.
// These can be used as needed, e.g., binding to the UI

        Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");
        return job;
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ERROR] 🔥 DB Error: " + ex.Message);
        throw;
    }
}
}
