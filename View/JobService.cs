using System;
using Npgsql;
using CivilProcessERP.Models.Job;

public class JobService
{
    private readonly string _connectionString = "Host=localhost;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

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
