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

                        await Task.Delay(delay, stoppingToken);
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

                // Run configured search queries (if any)
                foreach (var q in _queries)
                {
                    try
                    {
                        var res = await crawler.SearchAsync(q, stoppingToken);
                        _logger.LogInformation("Results for '{Query}':\n{Results}", q, res);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing query '{Query}'", q);
                    }
                }

                // Additionally, once per scheduled run, read job sites from shared files and crawl them.
                try
                {
                    await foreach (var site in ScrappingJobs.ReadJobSitesFromShared(solutionRoot).WithCancellation(stoppingToken))
                    {
                        try
                        {
                            if (site.Contains("fueled.com", StringComparison.OrdinalIgnoreCase))
                            {
                                // use injected singleton IBrowser to create a context
                                var browser = scope.ServiceProvider.GetRequiredService<IBrowser>();
                                await using var context = await browser.NewContextAsync();

                                var jobs = await AgentsAPI.Scrapers.Crawlers.FueledCrawler.CrawlFueledAsync(context);

                                foreach (var job in jobs)
                                {
                                    try
                                    {
                                        await jobRepo.AddOrUpdateAsync(job);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error saving job {ApplyUrl}", job.ApplyUrl);
                                    }
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
