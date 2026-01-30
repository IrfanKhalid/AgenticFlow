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
                eb.HasKey(j=>j.Id); // No primary key
                eb.Property(j => j.ApplyUrl); // Use ApplyUrl as unique key for now
                eb.Property(j => j.Title).HasMaxLength(1000);
                eb.Property(j => j.Location).HasMaxLength(500);

                // Store long text fields as text
                eb.Property(j => j.Description).HasColumnType("text");
                eb.Property(j => j.Responsibilities).HasColumnType("text");
                eb.Property(j => j.Achievements).HasColumnType("text");
                eb.Property(j => j.Requirements).HasColumnType("text");
                eb.Property(j => j.Compensation).HasColumnType("text");
                eb.Property(j => j.StartDate).HasColumnType("date");
                eb.Property(j => j.Active).HasColumnType("boolean");

            });
        }
    }
}
