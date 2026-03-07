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
        public DbSet<CrawlerRun> CrawlerRuns { get; set; } = null!;
        public DbSet<CrawlerLog> CrawlerLogs { get; set; } = null!;
        public DbSet<CronCrawler> CronCrawlers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobDetail>(eb =>
            {
                eb.HasKey(j=>j.Id); // No primary key
                eb.Property(j => j.ApplyUrl); // Use ApplyUrl as unique key for now
                eb.Property(j => j.Title).HasMaxLength(1000);
                eb.Property(j => j.Location).HasMaxLength(1000);
                eb.Property(j => j.CrawlerName).HasMaxLength(200);

                // Store long text fields as text
                eb.Property(j => j.Description).HasColumnType("text");
                eb.Property(j => j.Responsibilities).HasColumnType("text");
                eb.Property(j => j.Achievements).HasColumnType("text");
                eb.Property(j => j.Requirements).HasColumnType("text");
                eb.Property(j => j.Compensation).HasColumnType("text");
                eb.Property(j => j.StartDate).HasColumnType("date");
                eb.Property(j => j.Active).HasColumnType("boolean");
                eb.Property(j => j.IsProcessed).HasDefaultValue(false);
                eb.Property(j => j.EffectiveDate).HasColumnType("timestamp with time zone");

            });

            modelBuilder.Entity<CrawlerRun>(eb =>
            {
                eb.HasKey(r => r.Id);
                eb.Property(r => r.CrawlerName).IsRequired();
                eb.Property(r => r.StartedAtUtc).HasColumnType("timestamp with time zone");
                eb.Property(r => r.CompletedAtUtc).HasColumnType("timestamp with time zone");
                eb.Property(r => r.DurationMs);
            });

            modelBuilder.Entity<CrawlerLog>(eb =>
            {
                eb.HasKey(l => l.Id);
                eb.Property(l => l.CrawlerName).IsRequired().HasMaxLength(200);
                eb.Property(l => l.Status).IsRequired().HasMaxLength(50);
                eb.Property(l => l.TimestampUtc).HasColumnType("timestamp with time zone");
                eb.Property(l => l.ErrorMessage).HasColumnType("text");
                eb.Property(l => l.StackTrace).HasColumnType("text");
            });

            modelBuilder.Entity<CronCrawler>(eb =>
            {
                eb.HasKey(c => c.Id);
                eb.Property(c => c.CrawlerName).IsRequired().HasMaxLength(200);
                eb.Property(c => c.CronExpression).IsRequired().HasMaxLength(100);
                eb.Property(c => c.IsActive).HasDefaultValue(true);
                eb.Property(c => c.LastRunTime).HasColumnType("timestamp with time zone");
                eb.Property(c => c.IsRunning).HasDefaultValue(false);

                // Seed all available crawlers with a daily schedule
                eb.HasData(
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"), CrawlerName = "Microsoft", CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"), CrawlerName = "Amazon",    CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003"), CrawlerName = "Google",    CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"), CrawlerName = "Fueled",    CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005"), CrawlerName = "AshbyHQ",   CronExpression = "0 0 1 * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000006"), CrawlerName = "Acquia",    CronExpression = "0 0 1 * *", IsActive = true }
                );
            });
        }
    }
}
