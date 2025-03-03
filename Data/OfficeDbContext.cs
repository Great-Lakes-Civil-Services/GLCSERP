using Microsoft.EntityFrameworkCore;
using CivilProcessERP.Models;

namespace CivilProcessERP.Data
{
    public class OfficeDbContext : DbContext
    {
        public OfficeDbContext(DbContextOptions<OfficeDbContext> options)
            : base(options)
        {
        }

        public DbSet<LeaseAgreement> LeaseAgreements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the TS column as a concurrency token.
            // If TS is a DateTime, we use ConcurrencyCheck.
            modelBuilder.Entity<LeaseAgreement>()
                .Property(e => e.TS)
                .IsConcurrencyToken();

            base.OnModelCreating(modelBuilder);
        }
    }
}
