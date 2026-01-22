using Microsoft.EntityFrameworkCore;
using AgentsAPI.Shared.Models;

namespace AgentsAPI.DataAccess.Models
{
    public class AgentsDbContext : DbContext
    {
        public AgentsDbContext(DbContextOptions<AgentsDbContext> options) : base(options)
        {
        }

        public DbSet<JobDetail> JobDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobDetail>(eb =>
            {
                eb.HasKey(j => j.ApplyUrl); // Use ApplyUrl as unique key for now
                eb.Property(j => j.Title).HasMaxLength(1000);
                eb.Property(j => j.Location).HasMaxLength(500);

                // Store lists as JSON columns
                eb.Property(j => j.Description).HasColumnType("jsonb");
                eb.Property(j => j.Responsibilities).HasColumnType("jsonb");
                eb.Property(j => j.Achievements).HasColumnType("jsonb");
                eb.Property(j => j.Requirements).HasColumnType("jsonb");
            });
        }
    }
}
