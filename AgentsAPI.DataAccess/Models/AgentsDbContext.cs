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
        public DbSet<ProcessingJob> ProcessingJobs { get; set; } = null!;
        public DbSet<JobFeature> JobFeatures { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobDetail>(eb =>
            {
                eb.HasKey(j => j.Id);
                eb.Property(j => j.ApplyUrl);
                eb.Property(j => j.Title).HasMaxLength(1000);
                eb.Property(j => j.Location).HasMaxLength(1000);
                eb.Property(j => j.CrawlerName).HasMaxLength(200);
                eb.Property(j => j.Description).HasColumnType("text");
                eb.Property(j => j.Responsibilities).HasColumnType("text");
                eb.Property(j => j.Achievements).HasColumnType("text");
                eb.Property(j => j.Requirements).HasColumnType("text");
                eb.Property(j => j.Compensation).HasColumnType("text");
                eb.Property(j => j.StartDate).HasColumnType("date");
                eb.Property(j => j.Active).HasColumnType("boolean");
                eb.Property(j => j.IsProcessed).HasDefaultValue(false);
                eb.Property(j => j.EffectiveDate).HasColumnType("timestamp with time zone");
                eb.Property(j => j.ContentHash)
                    .HasColumnType("text")
                    .HasComputedColumnSql(
                        "md5(coalesce(\"Title\", '') || '|' || coalesce(\"Description\", '') || '|' || coalesce(\"ApplyUrl\", ''))",
                        stored: true);
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

                eb.HasData(
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0001-0000-0000-000000000001"), CrawlerName = "Microsoft", CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0002-0000-0000-000000000002"), CrawlerName = "Amazon", CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0003-0000-0000-000000000003"), CrawlerName = "Google", CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0004-0000-0000-000000000004"), CrawlerName = "Fueled", CronExpression = "0 0 * * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0005-0000-0000-000000000005"), CrawlerName = "AshbyHQ", CronExpression = "0 0 1 * *", IsActive = true },
                    new CronCrawler { Id = Guid.Parse("a1b2c3d4-0006-0000-0000-000000000006"), CrawlerName = "Acquia", CronExpression = "0 0 1 * *", IsActive = true }
                );
            });

            modelBuilder.Entity<ProcessingJob>(eb =>
            {
                eb.HasKey(p => p.ContentHash);
                eb.Property(p => p.ContentHash).IsRequired().HasColumnType("text");
                eb.Property(p => p.Title).IsRequired().HasMaxLength(1000);
                eb.Property(p => p.Location).IsRequired().HasMaxLength(1000);
                eb.Property(p => p.Description).HasColumnType("text");
                eb.Property(p => p.ApplyUrl).IsRequired().HasColumnType("text");
                eb.Property(p => p.ExecutedAt)
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();
                eb.Property(p => p.IsProcessd)
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
            });

            modelBuilder.Entity<JobFeature>(eb =>
            {
                eb.ToTable("JobFeatures");
                eb.HasKey(j => j.ContentHash);
                eb.Property(j => j.ContentHash)
                    .HasColumnName("content_hash")
                    .IsRequired()
                    .HasMaxLength(255);
                eb.Property(j => j.RequiredYears)
                    .HasColumnName("required_years")
                    .HasColumnType("integer");
                eb.Property(j => j.Keywords)
                    .HasColumnName("keywords")
                    .HasColumnType("text");
                eb.Property(j => j.Skills)
                    .HasColumnName("skills")
                    .HasColumnType("text");
                eb.Property(j => j.Tools)
                    .HasColumnName("tools")
                    .HasColumnType("text");
                eb.Property(j => j.CloudDemand)
                    .HasColumnName("cloud_demand")
                    .HasColumnType("text");
                eb.Property(j => j.AiDemand)
                    .HasColumnName("ai_demand")
                    .HasColumnType("text");
                eb.Property(j => j.Salary)
                    .HasColumnName("salary")
                    .HasColumnType("text");
                eb.Property(j => j.HasAi)
                    .HasColumnName("has_ai")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
                eb.Property(j => j.HasCloud)
                    .HasColumnName("has_cloud")
                    .HasColumnType("boolean")
                    .HasDefaultValue(false);
                eb.Property(j => j.ExecutedAt)
                    .HasColumnName("executed_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();
            });
        }
    }
}
