using System;
using Npgsql;  // Use Npgsql for PostgreSQL
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Data
{
    public class JobRepository
    {
        private readonly string connectionString = "Host=your_host;Port=5432;Database=your_db;Username=your_user;Password=your_password";

        public Job GetJobDetails(string jobNumber)
        {
            Job job = new Job();
            string query = "SELECT * FROM Jobs WHERE JobNumber = @JobNumber";

            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@JobNumber", jobNumber);

                conn.Open();
                NpgsqlDataReader reader = cmd.ExecuteReader();
                
                if (reader.Read())
                {
                    job = new Job
                    {
                        JobNumber = reader["JobNumber"]?.ToString() ?? string.Empty,
                        ClientRef = reader["ClientRef"]?.ToString() ?? string.Empty,
                        Attorney = reader["Attorney"]?.ToString() ?? string.Empty,
                        TypeOfService = reader["TypeOfService"]?.ToString() ?? string.Empty,
                        DateOfService = reader["DateOfService"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfService"]) : (DateTime?)null,
                        Status = reader["Status"]?.ToString() ?? string.Empty
                    };
                }
            }
            return job;
        }
    }
}
