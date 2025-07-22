using System;
using Npgsql;  // Use Npgsql for PostgreSQL
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Data
{
    public class JobRepository
    {
        private readonly string connectionString = "Host=192.168.0.15;Port=5432;Database=mypg_database;Username=postgres;Password=7866";

        public Job GetJobDetails(string jobNumber)
        {
            Job job = new Job();
            
            // Use the same query structure as JobService for consistency
            string query = "SELECT serialnum, caseserialnum FROM papers WHERE serialnum = @JobNumber";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@JobNumber", long.Parse(jobNumber));

                conn.Open();
                NpgsqlDataReader reader = cmd.ExecuteReader();
                
                if (reader.Read())
                {
                    job = new Job
                    {
                        JobId = reader["serialnum"]?.ToString() ?? string.Empty,
                        JobNumber = reader["serialnum"]?.ToString() ?? string.Empty, // Keep both for compatibility
                        CaseNumber = reader["caseserialnum"]?.ToString() ?? string.Empty,
                        Status = "Active" // Default status
                    };
                }
            }
            return job;
        }
    }
}
