using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using AgentsAPI.Shared.Models;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class AcquiaCrawler
    {
        public static async Task<List<JobDetail>> CrawlAcquiaAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync("https://www.acquia.com/careers/open-positions");

                var jobs = await page.EvaluateAsync<string[]>(@"
                    () => Array.from(
                        document.querySelectorAll('.views-row a')
                    ).map(a => a.href)
                ");
                foreach (var job in jobs)
                {
                    try
                    {
                        // navigate to job page
                        await page.GotoAsync(job);
                        await page.WaitForTimeoutAsync(700);

                        var jd = new JobDetail
                        {
                            EffectiveDate = DateTime.UtcNow,
                            CrawlerName = "Acquia"
                        };

                        // Title
                        jd.Title = await page.Locator("h1.section-header").InnerTextAsync();

                        // Location
                        var locationEl = await page.QuerySelectorAsync("p:has-text(\"Location:\")");
                        jd.Location = await page.Locator("div.job__location").First.InnerTextAsync(); ;
                        jd.Description = await page.InnerTextAsync("div.job__description.body");
                        jd.ApplyUrl = job;
                        await page.WaitForTimeoutAsync(600);
                        results.Add(jd);
                    }
                    catch (Exception ex)
                    {
                        {
                            // ignore single job failures
                        }
                    }
                }
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }
    }
}
