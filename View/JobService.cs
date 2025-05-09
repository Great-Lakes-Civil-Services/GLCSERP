using System;
using Npgsql;
using CivilProcessERP.Models.Job;
using static CivilProcessERP.Models.Job.InvoiceModel;
using CivilProcessERP.Models.Job; // Ensure this namespace contains PaymentEntryModel
// using CivilProcessERP.Models.Attachment; // Removed as the namespace does not exist
// Removed invalid namespace reference as 'Payment' does not exist in 'CivilProcessERP.Models'
 // Removed as the namespace 'Payment' does not exist


// Ensure JobLineItem is defined in the namespace CivilProcessERP.Models.Job

using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Controls; // Added for ListView
using System.IO; // Added for Path class
using System.Diagnostics; // Added for ProcessStartInfo
using System.Windows; // Added for MessageBox

public class JobService : INotifyPropertyChanged
{
    // Define AttachmentsListView as a property or field
    public ListView AttachmentsListView { get; set; }
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

    public List<AttachmentModel> Attachments { get; set; } = new List<AttachmentModel>();

    //public List<InvoiceModel> InvoiceEntries { get; set; } = new List<InvoiceModel>();

    //public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;

    public event PropertyChangedEventHandler PropertyChanged;

//     public decimal TotalInvoiceAmounts
//     {
//         get
//         {
//             return InvoiceEntries?.Sum(x => x.Amount) ?? 0;
//         }
//     }

//     public void RecalculateTotals()
// {
//     OnPropertyChanged(nameof(TotalInvoiceAmounts));
//     //OnPropertyChanged(nameof(TotalPaymentsAmount));
// }

//public decimal TotalInvoiceAmount => InvoiceEntries?.Sum(x => x.Amount) ?? 0;


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
    reader1.Close();
    job.CaseNumber = caseSerial;
}

// Step 2: Fetch Court number (courtnum) from cases table
string courtNum = null;
using (var cmd2 = new NpgsqlCommand("SELECT courtnum FROM cases WHERE serialnum=@caseserialnum", conn))
{
    cmd2.Parameters.AddWithValue("caseserialnum", int.Parse(job.CaseNumber));
    using var reader2 = cmd2.ExecuteReader();
    if (reader2.Read())
    {
        courtNum = reader2["courtnum"]?.ToString();
    }
    reader2.Close();
}

// Step 3: Fetch Court name from courts table based on courtnum
if (!string.IsNullOrEmpty(courtNum))
{
    using (var cmd24 = new NpgsqlCommand("SELECT name FROM courts WHERE serialnum = @courtnum", conn))
    {
        cmd24.Parameters.AddWithValue("courtnum", int.Parse(courtNum));
        using var reader24 = cmd24.ExecuteReader();
        if (reader24.Read())
        {
            job.Court = reader24["name"]?.ToString(); // Assign the court name to the Job object
        }
        reader24.Close();
    }
}

Console.WriteLine($"[INFO] ✅ Job fetched from DB: {job.JobId}, Court: {job.Court}, Defendant: {job.Defendant}");

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

// if (!string.IsNullOrEmpty(caseSerialNum))
// {
//     using (var cmd21 = new NpgsqlCommand("SELECT \"status\" FROM entity WHERE \"SerialNum\" = @caseSerialNum", conn))
//     {
//         cmd21.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerialNum));
//         using var reader21 = cmd21.ExecuteReader();
//         if (reader21.Read())
//         {
//             job.ClientStatus = reader21["status"]?.ToString();  // ✅ assign full name
//         }
//         reader21.Close();
//     }
// }

using (var cmd26 = new NpgsqlCommand("SELECT clientnum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd26.Parameters.AddWithValue("jobId", long.Parse(job.JobId));  // Pass the job ID to query the papers table
    using var reader26 = cmd26.ExecuteReader();
    if (reader26.Read())
    {
        // Retrieve the clientnum from the papers table
        var clientnum = reader26["clientnum"]?.ToString();
        reader26.Close();

        // Now use the clientnum to get the status from the entity table
        if (!string.IsNullOrEmpty(clientnum))
        {
            using (var cmd21 = new NpgsqlCommand("SELECT \"status\" FROM entity WHERE \"SerialNum\" = @clientnum", conn))
            {
                cmd21.Parameters.AddWithValue("clientnum", int.Parse(clientnum));  // Use the clientnum to fetch the status
                using var reader21 = cmd21.ExecuteReader();
                if (reader21.Read())
                {
                    job.ClientStatus = reader21["status"]?.ToString();  // Assign the status to the job object
                }
                reader21.Close();
            }
        }
    }
    reader26.Close();
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

// Debug: Show raw input
Console.WriteLine($"Raw Value (courtdatecode): {courtDateCodeRaw}");

// ✅ THE CORRECT OFFSET YOU DERIVED IN SQL
const long timestampOffset = 1225178692;  // From your SQL calculation

// --- COURT DATE ---
if (long.TryParse(courtDateCodeRaw, out long courtTimestamp) && courtTimestamp != 0)
{
    var correctedCourtTimestamp = courtTimestamp + timestampOffset;
    var courtDateTime = DateTimeOffset.FromUnixTimeSeconds(correctedCourtTimestamp).UtcDateTime;  // returns UTC
    Console.WriteLine($"Final UTC court date: {courtDateTime}");

    // Optional: convert to local time if needed
    // courtDateTime = courtDateTime.ToLocalTime();

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
    var servedDateTime = DateTimeOffset.FromUnixTimeSeconds(correctedServedTimestamp).UtcDateTime; // returns UTC
    Console.WriteLine($"Final UTC served date: {servedDateTime}");

    // Optional: convert to local time if needed
    // servedDateTime = servedDateTime.ToLocalTime();

    job.ServiceDate = servedDateTime.ToString("MM/dd/yyyy");
    job.ServiceTime = servedDateTime.ToString("h:mm tt");
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
       // OnPropertyChanged(nameof(TotalInvoiceAmount));
        // Removed JobDetailsView.RecalculateTotals(); as it is not defined in the current context
        // Ensure to implement recalculation logic elsewhere if needed
        // OnPropertyChanged(nameof(TotalInvoiceAmount));
        // RecalculateTotals();
        

    }
    // Console.WriteLine("Total Invoice Amount: " + TotalInvoiceAmount);
    // OnPropertyChanged(nameof(TotalInvoiceAmount));

    OnPropertyChanged(nameof(job.TotalInvoiceAmount));
    OnPropertyChanged(nameof(job.TotalPaymentsAmount));

    reader23.Close();

    Console.WriteLine($"Description: {description}, Quantity: {quantity}, Rate: {rate}, Amount: {amount}");
    //job.TotalInvoiceAmount = TotalInvoiceAmount;
}

using (var cmdPayment = new NpgsqlCommand("SELECT date, method, description, amount FROM payment WHERE jobnumber = @jobId", conn))
{
    cmdPayment.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var readerPayment = cmdPayment.ExecuteReader();
    while (readerPayment.Read())
    {
        // Format Date
        //var date = readerPayment["date"] != DBNull.Value ? Convert.ToDateTime(readerPayment["date"]).ToString("yyyy-MM-dd") : DateTime.MinValue.ToString("yyyy-MM-dd");
        
        // Format Time (if it's not null, else set default)
        // var time = readerPayment["time"] != DBNull.Value ? Convert.ToDateTime(readerPayment["time"]).ToString("HH:mm:ss") : "00:00:00";

        var dateTimeRaw = readerPayment["date"] != DBNull.Value ? Convert.ToDateTime(readerPayment["date"]) : DateTime.MinValue;

// Split the date and time into separate variables
var extractedDate = dateTimeRaw.ToString("yyyy-MM-dd");  // Extracts the date part
var extractedTime = dateTimeRaw.ToString("HH:mm:ss");    // Extracts the time part

// You can also use these values for further operations if needed.
Console.WriteLine($"Date: {extractedDate}, Time: {extractedTime}");
        
        var payment = new PaymentModel
        {
            Date = DateTime.Parse(extractedDate),  // Convert string to DateTime
            TimeOnly = extractedTime,  // Time in "HH:mm:ss" format
            Method = readerPayment["method"]?.ToString(),
            Description = readerPayment["description"]?.ToString(),
            Amount = readerPayment["amount"] != DBNull.Value ? Convert.ToDecimal(readerPayment["amount"]) : 0m
        };

        job.Payments.Add(payment);  // Assuming Payments is an ObservableCollection in the Job model
    }
    readerPayment.Close();
}
// Removed redundant declaration of timestampOffset
using (var cmd = new NpgsqlCommand(@"SELECT 
                                        comment, datetime, source, isattempt, 
                                        printonaff, printonfs, reviewed 
                                        FROM comments 
                                        WHERE serialnum = @jobId AND isattempt = false", conn))
{
    cmd.Parameters.AddWithValue("jobId", long.Parse(jobId));
    using var reader = cmd.ExecuteReader();

    while (reader.Read())
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

    reader.Close();
}

using (var cmd25 = new NpgsqlCommand(@"SELECT 
                                        comment, datetime, source, isattempt, 
                                        printonaff, printonfs, reviewed 
                                        FROM comments 
                                        WHERE serialnum = @jobId AND isattempt = true", conn))
{
    cmd25.Parameters.AddWithValue("jobId", long.Parse(jobId));
    using var reader25 = cmd25.ExecuteReader();

    while (reader25.Read())
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

    reader25.Close();
}



string address1 = null;
string address2 = null;
string state = null;
string city = null;
string zip = null;

// Query to get address details from serveedetails table
using (var cmd26 = new NpgsqlCommand("SELECT address1, address2, state, city, zip FROM serveedetails WHERE serialnum = @jobId AND seqnum = 1", conn))
{
    cmd26.Parameters.AddWithValue("jobId", long.Parse(job.JobId)); // Job ID from the papers table

    using var reader26 = cmd26.ExecuteReader();
    if (reader26.Read())
    {
        address1 = reader26["address1"]?.ToString();
        address2 = reader26["address2"]?.ToString();
        state = reader26["state"]?.ToString();
        city = reader26["city"]?.ToString();
        zip = reader26["zip"]?.ToString();
    }
    reader26.Close();
}


using (var cmd27 = new NpgsqlCommand("SELECT dudeservedlfm FROM papers WHERE serialnum = @jobId", conn))
{
    cmd27.Parameters.AddWithValue("jobId", long.Parse(jobId)); 
    using var reader27 = cmd27.ExecuteReader();

    if (!reader27.Read())
    {
        Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
        return null;
    }

    // This line was causing the error
    // job.JobId = reader27["serialnum"].ToString(); ❌

   // job.JobId = jobId; // ✅ Use the jobId you already passed in
    job.Plaintiffs = reader27["dudeservedlfm"]?.ToString()?.Trim() ?? "";

    reader27.Close();
}



using (var cmd28 = new NpgsqlCommand("SELECT pliannum FROM papers WHERE serialnum = @jobId", conn))
{
    cmd28.Parameters.AddWithValue("jobId", long.Parse(job.JobId));
    using var reader28 = cmd28.ExecuteReader();

    string pliannum = null;

    if (reader28.Read())
    {
        pliannum = reader28["pliannum"]?.ToString();
    }
    reader28.Close();

    if (!string.IsNullOrEmpty(pliannum))
    {
        using var cmd29 = new NpgsqlCommand(
            "SELECT \"firstname\", \"lastname\" FROM serveedetails WHERE \"serialnum\" = @pliannum AND \"seqnum\" = 1", conn);
        cmd29.Parameters.AddWithValue("pliannum", int.Parse(pliannum));

        using var reader29 = cmd29.ExecuteReader();
        if (reader29.Read())
        {
            var firstName = reader29["FirstName"]?.ToString()?.Trim();
            var lastName = reader29["LastName"]?.ToString()?.Trim();
            job.Plaintiff = $"{firstName} {lastName}".Trim();
        }
        reader29.Close();
    }
}

using (var cmd31 = new NpgsqlCommand("SELECT a.id AS attachment_id, a.description, a.purpose, bm.fileextension, bm.id AS blobmetadata_id FROM attachments a JOIN papersattachmentscross pac ON pac.attachmentid = a.id JOIN blobmetadata bm ON bm.changenum = a.changenum WHERE pac.paperserialnum = @serialnum;", conn))
{
    // Convert jobId to integer if needed
    int serialnum = Convert.ToInt32(jobId); // Ensure that jobId is integer
    cmd31.Parameters.AddWithValue("serialnum", serialnum); // Pass the integer serialnum

    using var reader31 = cmd31.ExecuteReader();
    while (reader31.Read())
    {
        var purpose = reader31["purpose"]?.ToString();
        var attachmentDescription = reader31["description"]?.ToString();
        var fileExtension = reader31["fileextension"]?.ToString();
        // Fetch the blob data as byte array using GetFieldValue<byte[]>() method
        //var blobData = reader31.GetFieldValue<byte[]>(reader31.GetOrdinal("file_data")); // Ensure the correct column name is used

        var blobMetadataId = reader31["blobmetadata_id"]?.ToString(); // Fetch the blobmetadata_id
         Console.WriteLine($"Purpose: {purpose}");
    Console.WriteLine($"Description: {attachmentDescription}");
    Console.WriteLine($"File Extension: {fileExtension}");
    

        

        // Add the data to the Attachments collection or directly bind it to the UI
       job.Attachments.Add(new AttachmentModel
{
    Purpose = purpose,
    Description = attachmentDescription ?? string.Empty,
    Format = fileExtension,
    BlobMetadataId = blobMetadataId
});


         Console.WriteLine($"Total Attachments: {Attachments.Count}");
    foreach (var attachment in Attachments)
    {
        Console.WriteLine($"Attachment: {attachment.Purpose}, {attachment.Description}");
    }
    }

    Console.WriteLine($"Attachments in Job: {job.Attachments.Count}");
foreach (var att in job.Attachments)
{
    Console.WriteLine($"Attachment: {att.Purpose}, {att.Description}, {att.Format}");
}

    reader31.Close();
}



    job.Address = $"{address1} {address2} {state} {city} {zip}".Trim(); // Trim to remove any extra spaces

// Log the concatenated address
Console.WriteLine($"Concatenated Address: {job.Address}");


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