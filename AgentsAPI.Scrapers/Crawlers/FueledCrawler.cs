using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright;
using AgentsAPI.Shared.Models;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class FueledCrawler
    {
        public static async Task<List<JobDetail>> CrawlFueledAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync("https://fueled.com/careers");
                await page.WaitForSelectorAsync("h2.wp-block-post-title a");

                var jobLinks = await page.QuerySelectorAllAsync("h2.wp-block-post-title a");
                var jobs = new List<(string Title, string Url)>();

                foreach (var job in jobLinks)
                {
                    jobs.Add((
                        Title: (await job.InnerTextAsync()).Trim(),
                        Url: await job.GetAttributeAsync("href")
                    ));
                }

                foreach (var job in jobs)
                {
                    try
                    {
                        // navigate to job page
                        await page.GotoAsync(job.Url);
                        await page.WaitForSelectorAsync(".entry-content");

                        var jd = new JobDetail
                        {
                            EffectiveDate = DateTime.UtcNow,
                            CrawlerName = "Fueled"
                        };

                        // Title
                        jd.Title = (await page.InnerTextAsync("h1")).Trim();

                        // Location
                        var locationEl = await page.QuerySelectorAsync("p:has-text(\"Location:\")");
                        jd.Location = locationEl != null ? (await locationEl.InnerTextAsync()).Replace("Location:", "").Trim() : string.Empty;

                        // Description: collect paragraphs and join into a single string
                        var descParts = new List<string>();
                        var paragraphs = await page.QuerySelectorAllAsync(".entry-content > p");
                        foreach (var p in paragraphs)
                        {
                            var text = (await p.InnerTextAsync()).Trim();
                            if (!text.StartsWith("Location:") && text.Length > 50)
                                descParts.Add(text);
                        }
                        jd.Description = descParts.Count > 0 ? string.Join("\n\n", descParts) : string.Empty;

                        // Sections: gather items and join
                        var responsibilities = new List<string>();
                        var achievements = new List<string>();
                        var requirements = new List<string>();

                        var headings = await page.QuerySelectorAllAsync("h3.wp-block-heading");
                        foreach (var heading in headings)
                        {
                            var sectionTitle = (await heading.InnerTextAsync()).Trim().ToLower();
                            var columnHandle = await heading.EvaluateHandleAsync("h => h.parentElement.nextElementSibling");
                            if (columnHandle == null) continue;

                            var items = await columnHandle.AsElement().QuerySelectorAllAsync("li");
                            foreach (var item in items)
                            {
                                var text = (await item.InnerTextAsync()).Trim();
                                if (sectionTitle.Contains("what you will do"))
                                    responsibilities.Add(text);
                                else if (sectionTitle.Contains("what you will achieve"))
                                    achievements.Add(text);
                                else if (sectionTitle.Contains("about you"))
                                    requirements.Add(text);
                            }
                        }

                        jd.Responsibilities = responsibilities.Count > 0 ? string.Join("\n", responsibilities) : string.Empty;
                        jd.Achievements = achievements.Count > 0 ? string.Join("\n", achievements) : string.Empty;
                        jd.Requirements = requirements.Count > 0 ? string.Join("\n", requirements) : string.Empty;

                        // Compensation
                        var compHeader = await page.QuerySelectorAsync("h3:has-text(\"Contractor fee\")");
                        if (compHeader != null)
                        {
                            var compParagraphHandle = await compHeader.EvaluateHandleAsync("h => h.nextElementSibling");
                            if (compParagraphHandle != null)
                            {
                                jd.Compensation = (await compParagraphHandle.AsElement().InnerTextAsync()).Trim();
                            }
                        }

                        // Apply URL
                        var applyLink = await page.QuerySelectorAsync("a:has-text(\"Apply Now\")");
                        jd.ApplyUrl = applyLink != null ? await applyLink.GetAttributeAsync("href") : string.Empty;
                        await page.WaitForTimeoutAsync(600);
                        results.Add(jd);
                    }
                    catch
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
