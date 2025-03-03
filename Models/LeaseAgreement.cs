using System;
using System.ComponentModel.DataAnnotations;

namespace CivilProcessERP.Models
{
    public class LeaseAgreement
    {
        public int Id { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Use the existing TS column as the concurrency token.
        // Here, TS is of type DateTime and marked with ConcurrencyCheck.
        [ConcurrencyCheck]
        public DateTime TS { get; set; }
    }
}
