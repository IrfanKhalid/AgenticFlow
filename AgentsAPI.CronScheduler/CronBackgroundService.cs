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
        private readonly string _cronExpression;
        private readonly List<string> _queries;
        public CronBackgroundService(IServiceProvider provider, ILogger<CronBackgroundService> logger)
        {
            _provider = provider;
            _logger = logger;

            // read configuration from environment variables for easier Windows Scheduler runs
            // default to once per day at midnight UTC

            _cronExpression = Environment.GetEnvironmentVariable("CRON_EXPRESSION") ?? "0 0 * * *";
            var queriesRaw = Environment.GetEnvironmentVariable("CRAWL_QUERIES") ?? "example";
            _queries = queriesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CronBackgroundService starting with cron '{Cron}' and queries: {Queries}", _cronExpression, _queries);

            var cron = CronExpression.Parse(_cronExpression, CronFormat.Standard);

            while (!stoppingToken.IsCancellationRequested)
            {
                var next = cron.GetNextOccurrence(DateTime.UtcNow);
                if (!next.HasValue)
                {
                    _logger.LogInformation("No next occurrence from cron expression. Stopping service.");
                    break;
                }

                var delay = next.Value - DateTime.UtcNow;
                if (delay.TotalMilliseconds > 0)
                {
                    try
                    {

                       // await Task.Delay(delay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                _logger.LogInformation("Triggering crawl at {Time}", DateTimeOffset.Now);

                using var scope = _provider.CreateScope();

                // Also get host environment to locate shared files
                var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
                var solutionRoot = env.ContentRootPath;

                // Get DB context options and create dbcontext

                var configConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
                var connectionString = configConn ?? "Host=localhost;Database=agentsdb;Username=postgres;Password=postgres";

                try
                {
                    using var playwright = await Playwright.CreateAsync();
                    await using var browser = await playwright.Chromium.LaunchAsync(
                        new BrowserTypeLaunchOptions
                        {
                            Headless = false,
                        });

                    var crawlerTasks = StartKnownCrawlers(browser, connectionString, stoppingToken).ToArray();
                    if (crawlerTasks.Length > 0)
                    {
                        _logger.LogInformation("Launching {Count} crawler(s) in parallel", crawlerTasks.Length);
                        await Task.WhenAll(crawlerTasks);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running crawler batch");
                }

                //await RunSequentialSiteOptimizations(solutionRoot, stoppingToken);
            }

            _logger.LogInformation("CronBackgroundService stopping.");
        }

        private IEnumerable<Task> StartKnownCrawlers(IBrowser browser, string connectionString, CancellationToken stoppingToken)
        {
            return new[]
            {
                //RunCrawlerAsync(browser, "Fueled", ctx => AgentsAPI.Scrapers.Crawlers.FueledCrawler.CrawlFueledAsync(ctx), connectionString, stoppingToken),
                //RunCrawlerAsync(browser, "AshbyHQ", ctx => AgentsAPI.Scrapers.Crawlers.AshbyhqCrawler.CrawlAshbyhqAsync(ctx), connectionString, stoppingToken),
                //RunCrawlerAsync(browser, "Acquia", ctx => AgentsAPI.Scrapers.Crawlers.AcquiaCrawler.CrawlAcquiaAsync(ctx), connectionString, stoppingToken),
                RunCrawlerAsync(browser, "Microsoft", ctx => AgentsAPI.Scrapers.Crawlers.MicrosoftCrawler.CrawlMicrosoftAsync(ctx), connectionString, stoppingToken),
                RunCrawlerAsync(browser, "Amazon", ctx => AgentsAPI.Scrapers.Crawlers.AmazonCrawler.CrawlAmazonAsync(ctx), connectionString, stoppingToken)
            };
        }

        private async Task RunCrawlerAsync(IBrowser browser, string crawlerName, Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc, string connectionString, CancellationToken stoppingToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var startedAt = DateTime.UtcNow;
            var crawlerRunId = Guid.NewGuid();

            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                _logger.LogInformation("Starting crawler {CrawlerName} (RunId: {RunId})", crawlerName, crawlerRunId);

                var jobs = await CrawlWithIsolatedContextAsync(browser, crawlFunc, stoppingToken);
                if (jobs?.Count > 0)
                {
                    await using var dbContext = CreateDbContext(connectionString);
                    var jobRepository = new JobRepository(dbContext);
                    await InsertJobsByBatch(jobs, jobRepository);
                    _logger.LogInformation("Crawler {CrawlerName} completed with {Count} jobs", crawlerName, jobs.Count);
                }
                else
                {
                    _logger.LogInformation("Crawler {CrawlerName} completed with no jobs", crawlerName);
                }

                stopwatch.Stop();
                await SaveCrawlerRunAsync(crawlerRunId, crawlerName, startedAt, DateTime.UtcNow, stopwatch.ElapsedMilliseconds, connectionString);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Crawler {CrawlerName} canceled", crawlerName);
                stopwatch.Stop();
                await SaveCrawlerRunAsync(crawlerRunId, crawlerName, startedAt, DateTime.UtcNow, stopwatch.ElapsedMilliseconds, connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Crawler {CrawlerName} failed", crawlerName);
                stopwatch.Stop();
                await SaveCrawlerRunAsync(crawlerRunId, crawlerName, startedAt, DateTime.UtcNow, stopwatch.ElapsedMilliseconds, connectionString);
            }
        }

        private async Task<List<JobDetail>> CrawlWithIsolatedContextAsync(IBrowser browser, Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc, CancellationToken stoppingToken)
        {
            await using var context = await browser.NewContextAsync();
            context.SetDefaultNavigationTimeout(1200000);
            context.SetDefaultTimeout(1200000);
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

        private async Task SaveCrawlerRunAsync(Guid id, string crawlerName, DateTime startedAt, DateTime completedAt, long durationMs, string connectionString)
        {
            try
            {
                await using var dbContext = CreateDbContext(connectionString);
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

        private static AgentsDbContext CreateDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new AgentsDbContext(optionsBuilder.Options);
        }

        private async Task RunSequentialSiteOptimizations(string solutionRoot, CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var site in ScrappingJobs.ReadJobSitesFromShared(solutionRoot).WithCancellation(stoppingToken))
                {
                    try
                    {
                        _logger.LogInformation("Optimizing crawler for site {Site}", site);
                        await ScrappingJobs.CrawlSitesAsync(site);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error optimizing crawler for site {Site}", site);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading job sites from shared files");
            }
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
                _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
            }
        }
    }
}
