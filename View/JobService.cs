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
        using var cmd1 = new NpgsqlCommand("SELECT serialnum, caseserialnum FROM papers WHERE serialnum = @jobId", conn);
        cmd1.Parameters.AddWithValue("jobId", long.Parse(jobId));  // Use long for large serialnums
        using var reader1 = cmd1.ExecuteReader();

        if (!reader1.Read())
        {
            Console.WriteLine("[WARN] ❌ No paper found with JobId: " + jobId);
            return null;
        }

        string caseSerial = reader1["caseserialnum"].ToString();
        job.JobId = reader1["serialnum"].ToString();

        reader1.Close();

        // Step 2: Fetch Court from cases table
        using var cmd2 = new NpgsqlCommand("SELECT typecourt FROM cases WHERE serialnum = @caseserialnum", conn);
        cmd2.Parameters.AddWithValue("caseserialnum", int.Parse(caseSerial));
        using var reader2 = cmd2.ExecuteReader();

        if (reader2.Read())
        {
            job.Court = reader2["typecourt"]?.ToString();
        }

        Console.WriteLine("[INFO] ✅ Job fetched from DB: " + job.JobId + ", Court: " + job.Court);
        return job;
    }
    catch (Exception ex)
    {
        Console.WriteLine("[ERROR] 🔥 DB Error: " + ex.Message);
        throw;
    }
}

}
