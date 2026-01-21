using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Playwright;

namespace AgentsAPI.CronScheduler
{
    public static class ScrappingJobs
    {
        // Reads the CSV from the Shared project and returns the list of websites (Website column)
        public static IEnumerable<string> ReadJobSitesFromShared(string solutionRoot)
        {
            // Path relative to solution root
            var relativePath = Path.Combine("AgentsAPI.Shared", "Files", "Job Sites.csv");
            var fullPath = Path.Combine(solutionRoot, relativePath);

            if (!File.Exists(fullPath))
                yield break;

            using var reader = new StreamReader(fullPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            // Read header and records
            var records = csv.GetRecords<dynamic>();
            foreach (var rec in records)
            {
                var dict = rec as IDictionary<string, object>;
                if (dict != null && dict.ContainsKey("Website") && dict["Website"] != null)
                {
                    var site = dict["Website"].ToString()!.Trim();
                    if (!string.IsNullOrWhiteSpace(site))
                        yield return site;
                }
            }
        }

        // Opens Playwright browser and navigates to each site sequentially
        public static async Task CrawlSitesAsync(IEnumerable<string> sites, Func<string, Task>? onSiteVisited = null)
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            var context = await browser.NewContextAsync();

            foreach (var site in sites)
            {
                try
                {
                    var page = await context.NewPageAsync();
                    await page.GotoAsync(site, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });

                    // Optionally wait for network idle for JS-heavy
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                    if (onSiteVisited != null)
                        await onSiteVisited(site);

                    await page.CloseAsync();
                }
                catch
                {
                    // swallow individual site errors
                }
            }

            await context.CloseAsync();
            await browser.CloseAsync();
        }
    }
}
