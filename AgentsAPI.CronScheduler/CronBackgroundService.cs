using AgentsAPI.DataAccess.Models;
using AgentsAPI.DataAccess.Repositories;
using AgentsAPI.Shared.Models;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentsAPI.CronScheduler
{
    public class CronBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<CronBackgroundService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

        public CronBackgroundService(IServiceProvider provider, ILogger<CronBackgroundService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CronBackgroundService started. Polling every {Interval}s", PollInterval.TotalSeconds);

            // On startup, reset any IsRunning flags left from a previous crash
            await ResetStaleRunningFlagsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var dueCrawlers = await GetDueCrawlersAsync();

                    if (dueCrawlers.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} crawler(s) due to run: {Names}",
                            dueCrawlers.Count, string.Join(", ", dueCrawlers.Select(c => c.CrawlerName)));

                        var tasks = new List<Task>();
                        foreach (var crawler in dueCrawlers)
                        {
                            if (!CrawlerRegistry.TryGet(crawler.CrawlerName, out var crawlFunc))
                            {
                                _logger.LogWarning("No implementation found for crawler '{Name}', skipping", crawler.CrawlerName);
                                continue;
                            }

                            // Try to acquire the lock (atomic UPDATE ... WHERE IsRunning = false)
                            if (!await TryAcquireLockAsync(crawler.Id))
                            {
                                _logger.LogInformation("Crawler '{Name}' is already running, skipping", crawler.CrawlerName);
                                continue;
                            }

                            tasks.Add(RunCrawlerWithLockAsync(crawler, crawlFunc, stoppingToken));
                        }

                        if (tasks.Count > 0)
                            await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in scheduler polling loop");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("CronBackgroundService stopping.");
        }

        /// <summary>
        /// Reads CronCrawlers where IsActive=true, IsRunning=false,
        /// and the cron expression indicates a run is due based on LastRunTime.
        /// </summary>
        private async Task<List<CronCrawler>> GetDueCrawlersAsync()
        {
            await using var db = CreateDbContext();
            var activeCrawlers = await db.CronCrawlers
                .Where(c => c.IsActive && !c.IsRunning)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var due = new List<CronCrawler>();

            foreach (var crawler in activeCrawlers)
            {
                try
                {
                    var cron = Cronos.CronExpression.Parse(crawler.CronExpression, CronFormat.Standard);

                    // If never run, it's due immediately
                    if (!crawler.LastRunTime.HasValue)
                    {
                        due.Add(crawler);
                        continue;
                    }

                    // Find the next occurrence after LastRunTime — if it's in the past or now, it's due
                    var nextOccurrence = cron.GetNextOccurrence(crawler.LastRunTime.Value);
                    if (nextOccurrence.HasValue && nextOccurrence.Value <= now)
                    {
                        due.Add(crawler);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid cron expression '{Cron}' for crawler '{Name}'",
                        crawler.CronExpression, crawler.CrawlerName);
                }
            }

            return due;
        }

        /// <summary>
        /// Atomically sets IsRunning=true only if it's currently false.
        /// Returns true if the lock was acquired (this instance owns execution).
        /// Uses ExecuteUpdate with a WHERE filter for atomicity.
        /// </summary>
        private async Task<bool> TryAcquireLockAsync(Guid crawlerId)
        {
            await using var db = CreateDbContext();
            var rowsAffected = await db.CronCrawlers
                .Where(c => c.Id == crawlerId && !c.IsRunning)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsRunning, true));

            return rowsAffected > 0;
        }

        /// <summary>
        /// Releases the lock and updates LastRunTime after a crawler completes.
        /// </summary>
        private async Task ReleaseLockAsync(Guid crawlerId)
        {
            try
            {
                await using var db = CreateDbContext();
                await db.CronCrawlers
                    .Where(c => c.Id == crawlerId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.IsRunning, false)
                        .SetProperty(c => c.LastRunTime, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock for crawler {Id}", crawlerId);
            }
        }

        /// <summary>
        /// On startup, reset any crawlers stuck with IsRunning=true
        /// (e.g. from a crash or forced shutdown).
        /// </summary>
        private async Task ResetStaleRunningFlagsAsync()
        {
            try
            {
                await using var db = CreateDbContext();
                var reset = await db.CronCrawlers
                    .Where(c => c.IsRunning)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsRunning, false));

                if (reset > 0)
                    _logger.LogWarning("Reset {Count} stale IsRunning flag(s) from previous run", reset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset stale running flags on startup");
            }
        }

        /// <summary>
        /// Wraps RunCrawlerAsync with lock acquire/release on the CronCrawler row.
        /// </summary>
        private async Task RunCrawlerWithLockAsync(CronCrawler crawler, Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc, CancellationToken stoppingToken)
        {
            try
            {
                await RunCrawlerAsync(crawler.CrawlerName, crawlFunc, stoppingToken);
            }
            finally
            {
                await ReleaseLockAsync(crawler.Id);
            }
        }

        private async Task RunCrawlerAsync(string crawlerName, Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc, CancellationToken stoppingToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var startedAt = DateTime.UtcNow;
            var crawlerRunId = Guid.NewGuid();
            var jobs = new List<JobDetail>();
            string status = "Success";
            string? errorMessage = null;
            string? stackTrace = null;
            int jobsSaved = 0;

            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                _logger.LogInformation("Starting crawler {CrawlerName} (RunId: {RunId})", crawlerName, crawlerRunId);

                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = true,
                    });

                jobs = await CrawlWithIsolatedContextAsync(browser, crawlFunc, stoppingToken);
                _logger.LogInformation("Crawler {CrawlerName} completed with {Count} jobs", crawlerName, jobs?.Count ?? 0);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                status = "Canceled";
                _logger.LogInformation("Crawler {CrawlerName} canceled", crawlerName);
            }
            catch (Exception ex)
            {
                status = "Error";
                errorMessage = ex.Message;
                stackTrace = ex.StackTrace;
                _logger.LogError(ex, "Crawler {CrawlerName} failed", crawlerName);
            }
            finally
            {
                stopwatch.Stop();

                // Always try to save whatever jobs were crawled, even on error
                if (jobs?.Count > 0)
                {
                    try
                    {
                        await using var dbContext = CreateDbContext();
                        var jobRepository = new JobRepository(dbContext);
                        await InsertJobsByBatch(jobs, jobRepository);
                        jobsSaved = jobs.Count;
                        _logger.LogInformation("Crawler {CrawlerName}: saved {Count} jobs", crawlerName, jobsSaved);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "Crawler {CrawlerName}: failed to save {Count} crawled jobs", crawlerName, jobs.Count);
                        if (status == "Success") status = "Error";
                        errorMessage = errorMessage == null
                            ? $"Save failed: {saveEx.Message}"
                            : $"{errorMessage} | Save failed: {saveEx.Message}";
                    }
                }

                // Save CrawlerRun
                await SaveCrawlerRunAsync(crawlerRunId, crawlerName, startedAt, DateTime.UtcNow, stopwatch.ElapsedMilliseconds);

                // Save CrawlerLog
                await SaveCrawlerLogAsync(crawlerName, startedAt, stopwatch.ElapsedMilliseconds,
                    status, jobs?.Count ?? 0, jobsSaved, errorMessage, stackTrace);
            }
        }

        private async Task<List<JobDetail>> CrawlWithIsolatedContextAsync(IBrowser browser, Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc, CancellationToken stoppingToken)
        {
            await using var context = await browser.NewContextAsync();
            context.SetDefaultNavigationTimeout(60000);
            context.SetDefaultTimeout(30000);
            await context.RouteAsync("**/*", route =>
            {
                var type = route.Request.ResourceType;
                return type == "image" || type == "font" || type == "media"
                    ? route.AbortAsync()
                    : route.ContinueAsync();
            });

            stoppingToken.ThrowIfCancellationRequested();
            return await crawlFunc(context);
        }

        private async Task SaveCrawlerRunAsync(Guid id, string crawlerName, DateTime startedAt, DateTime completedAt, long durationMs)
        {
            try
            {
                await using var dbContext = CreateDbContext();
                var crawlerRun = new CrawlerRun
                {
                    Id = id,
                    CrawlerName = crawlerName,
                    StartedAtUtc = startedAt,
                    CompletedAtUtc = completedAt,
                    DurationMs = durationMs
                };

                dbContext.CrawlerRuns.Add(crawlerRun);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Saved crawler run for {CrawlerName}: Duration {DurationMs}ms", crawlerName, durationMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save crawler run for {CrawlerName}", crawlerName);
            }
        }

        private async Task SaveCrawlerLogAsync(string crawlerName, DateTime startedAt, long durationMs,
            string status, int jobsCrawled, int jobsSaved, string? errorMessage, string? stackTrace)
        {
            try
            {
                await using var dbContext = CreateDbContext();
                var log = new CrawlerLog
                {
                    CrawlerName = crawlerName,
                    TimestampUtc = startedAt,
                    DurationMs = durationMs,
                    Status = status,
                    JobsCrawled = jobsCrawled,
                    JobsSaved = jobsSaved,
                    ErrorMessage = errorMessage,
                    StackTrace = stackTrace
                };

                dbContext.CrawlerLogs.Add(log);
                await dbContext.SaveChangesAsync();

                _logger.LogInformation("Saved crawler log for {CrawlerName}: Status={Status}, Crawled={Crawled}, Saved={Saved}",
                    crawlerName, status, jobsCrawled, jobsSaved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save crawler log for {CrawlerName}", crawlerName);
            }
        }

        private static AgentsDbContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
            optionsBuilder.UseNpgsql(DbConnectionStringProvider.GetPostgres());
            return new AgentsDbContext(optionsBuilder.Options);
        }

        private async Task InsertJobsByBatch(List<JobDetail> jobs, JobRepository jobRepository)
        {
            try
            {
                if (jobs.Count < 1000)
                {
                    await jobRepository.AddOrUpdateAsync(jobs);
                }
                else
                {
                    var batchSize = 1000;
                    for (int i = 0; i < jobs.Count; i += batchSize)
                    {
                        var batch = jobs.GetRange(i, Math.Min(batchSize, jobs.Count - i));
                        await jobRepository.AddOrUpdateAsync(batch);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving jobs");
            }
        }
    }
}
