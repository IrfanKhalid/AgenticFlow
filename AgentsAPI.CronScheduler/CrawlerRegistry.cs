using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentsAPI.CronScheduler
{
    /// <summary>
    /// Maps crawler names (as stored in CronCrawlers table) to their crawl functions.
    /// Add new crawlers here and insert a matching row in CronCrawlers to enable them.
    /// </summary>
    public static class CrawlerRegistry
    {
        private static readonly Dictionary<string, Func<IBrowserContext, Task<List<JobDetail>>>> _crawlers = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Microsoft"] = ctx => Scrapers.Crawlers.MicrosoftCrawler.CrawlMicrosoftAsync(ctx),
            ["Amazon"]    = ctx => Scrapers.Crawlers.AmazonCrawler.CrawlAmazonAsync(ctx),
            ["Google"]    = ctx => Scrapers.Crawlers.GoogleCrawler.CrawlGoogleAsync(ctx),
            ["Fueled"]    = ctx => Scrapers.Crawlers.FueledCrawler.CrawlFueledAsync(ctx),
            ["AshbyHQ"]   = ctx => Scrapers.Crawlers.AshbyhqCrawler.CrawlAshbyhqAsync(ctx),
            ["Acquia"]    = ctx => Scrapers.Crawlers.AcquiaCrawler.CrawlAcquiaAsync(ctx),
        };

        public static bool TryGet(string crawlerName, out Func<IBrowserContext, Task<List<JobDetail>>> crawlFunc)
            => _crawlers.TryGetValue(crawlerName, out crawlFunc!);
    }
}
