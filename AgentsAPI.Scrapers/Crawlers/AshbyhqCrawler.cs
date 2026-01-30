using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using AgentsAPI.Shared.Models;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class AshbyhqCrawler
    {
        public static async Task<List<JobDetail>> CrawlAshbyhqAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                var root = "https://jobs.ashbyhq.com/1password";
                await page.GotoAsync(root);

                await page.WaitForSelectorAsync("div.ashby-job-posting-brief-list");
                var links = await page.EvaluateAsync<string[]>(@"
                    () => Array.from(
                        document.querySelectorAll('div.ashby-job-posting-brief-list a[href]')
                    ).map(a => a.href)
                ");


                foreach (var job in links)
                {
                try
                    {
                        // navigate to job page
                        await page.GotoAsync(job);
                        
                        var jd = new JobDetail();

                        // Title
                        jd.Title = await page.Locator("h1.ashby-job-posting-heading").InnerTextAsync();

                        // Location
                        jd.Location = await page.Locator("h2:has-text('Location') + p").First.InnerTextAsync();

                        // Description: collect paragraphs and join into a single string
                        var descParts = new List<string>();

                        jd.Description = descParts.Count > 0 ? string.Join("\n\n", descParts) : string.Empty;
                        var rawText = await page
                            .Locator("div._descriptionText_oj0x8_198")
                            .InnerTextAsync();

                        var cleaned = string.Join(
                            Environment.NewLine + Environment.NewLine,
                            rawText
                                .Split('\n')
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                        );
                        jd.Description = cleaned;
                        jd.ApplyUrl = job;

                        results.Add(jd);
                    }
                    catch(Exception ex)
                    {
                        // ignore single job failures
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
