using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgentsAPI.DataAccess.Repositories;
using AgentsAPI.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

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

            while (!stoppingToken.IsCancellationRequested || true)
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

                var crawler = scope.ServiceProvider.GetRequiredService<AgentsAPI.Agents.ICrawlerAgent>();
                // Also get host environment to locate shared files
                var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
                var solutionRoot = env.ContentRootPath;

                // Get DB context options and create dbcontext
                var configConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION");
                var connectionString = configConn ?? "Host=localhost;Database=agentsdb;Username=postgres;Password=postgres";
                var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
                optionsBuilder.UseNpgsql(connectionString);

                using var dbContext = new AgentsDbContext(optionsBuilder.Options);
                var jobRepo = new JobRepository(dbContext);
                try
                {
                    await foreach (var site in ScrappingJobs.ReadJobSitesFromShared(solutionRoot).WithCancellation(stoppingToken))
                    {
                        try
                        {
                            var playwright = await Playwright.CreateAsync();

                            var browser = await playwright.Chromium.LaunchAsync(
                                new BrowserTypeLaunchOptions
                                {
                                    Headless = true
                                });
                            await using var context = await browser.NewContextAsync();
                            if (site.Contains("fueled.com", StringComparison.OrdinalIgnoreCase))
                            {
                                // use injected singleton IBrowser to create a context
                                

                                var jobs = await AgentsAPI.Scrapers.Crawlers.FueledCrawler.CrawlFueledAsync(context);

                                try
                                {
                                    await jobRepo.AddOrUpdateAsync(jobs);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
                                }
                            }
                            else if (site.Contains("ashbyhq", StringComparison.OrdinalIgnoreCase))
                            {
                                var jobs = await AgentsAPI.Scrapers.Crawlers.AshbyhqCrawler.CrawlAshbyhqAsync(context);

                                try
                                {
                                    await jobRepo.AddOrUpdateAsync(jobs);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
                                }
                            }
                            else if (site.Contains("acquia", StringComparison.OrdinalIgnoreCase))
                            {
                                var jobs = await AgentsAPI.Scrapers.Crawlers.AcquiaCrawler.CrawlAcquiaAsync(context);

                                try
                                {
                                    await jobRepo.AddOrUpdateAsync(jobs);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
                                }
                            }
                            else if (site.Contains("microsoft", StringComparison.OrdinalIgnoreCase))
                            {
                                var jobs = await AgentsAPI.Scrapers.Crawlers.MicrosoftCrawler.CrawlMicrosoftAsync(context);
                                try
                                {
                                    await jobRepo.AddOrUpdateAsync(jobs);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error saving job {ApplyUrl}", jobs);
                                }
                            }
                            else
                            {
                                // fallback generic crawl (not implemented) - use previous CrawlSitesAsync
                                await ScrappingJobs.CrawlSitesAsync(site);
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error crawling job site {Site}", site);
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

            _logger.LogInformation("CronBackgroundService stopping.");
        }
    }
}
