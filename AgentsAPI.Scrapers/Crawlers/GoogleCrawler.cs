using AgentsAPI.Scrapers.Crawlers.Utility;
using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class GoogleCrawler
    {
        private const string BaseUri = "https://www.google.com/about/careers/applications/";

        public static async Task<List<JobDetail>> CrawlGoogleAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync(
                    "https://www.google.com/about/careers/applications/jobs/results");
                await page.Locator("div.VfPpkd-dgl2Hf-ppHlrf-sM5MNb >> a[href*='jobs/results']").First.ClickAsync();
                // Wait for the job list to render
                //await page.WaitForSelectorAsync("li.zE6MFb", new PageWaitForSelectorOptions { Timeout = 60000 });
                await repoUtility.PoliteDelayAsync(200, 600);
                while (true)
                {
                    // Wait for job items on the current page
                    //await page.WaitForSelectorAsync("ul.uT61wd li.zE6MFb");
                    // Most reliable - using the parent div that contains both button and link
                    
                    // Collect all job link hrefs + titles from the listing sidebar
                    var jobItems = page.Locator("li.zE6MFb");
                    var count = await jobItems.CountAsync();

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var item = jobItems.Nth(i);
                            var anchor = item.Locator("a.Si6A0c");
                            await repoUtility.PoliteDelayAsync(300, 700);
                            // Click the job item to load its detail in the right pane
                            await anchor.ScrollIntoViewIfNeededAsync();
                            await anchor.ClickAsync();

                            // Wait for the detail pane to load with the job title
                            await page.WaitForSelectorAsync("h2.p1N2lc", new PageWaitForSelectorOptions { Timeout = 30000 });

                            // Verify the detail pane content has updated by checking the data-id matches
                            var dataId = await item.GetAttributeAsync("data-id") ?? "";
                            await page.WaitForSelectorAsync($"div.DkhPwc[data-id='{dataId}']",
                                new PageWaitForSelectorOptions { Timeout = 15000 });

                            var jd = await ExtractJobDetailAsync(page);
                            if (!string.IsNullOrEmpty(jd.Title))
                            {
                                results.Add(jd);
                            }
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Error processing job item {i}: {ex.Message}");  
                            // ignore single job failures, continue to next
                        }
                    }

                    // Pagination block replacement
                    var nextLink = page.Locator("a[aria-label='Go to next page']").Last;
                    if (await nextLink.CountAsync() == 0)
                        break;

                    // Anchor-based disabled check
                    var ariaDisabled = await nextLink.GetAttributeAsync("aria-disabled");
                    if (string.Equals(ariaDisabled, "true", StringComparison.OrdinalIgnoreCase))
                        break;

                    var href = await nextLink.GetAttributeAsync("href");
                    if (string.IsNullOrWhiteSpace(href))
                        break;

                    var nextUrl = href.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? href
                        : new Uri(new Uri(BaseUri), href).ToString();

                    await page.GotoAsync(nextUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 30000
                    });

                    await page.WaitForSelectorAsync("li.zE6MFb", new PageWaitForSelectorOptions
                    {
                        Timeout = 30000
                    });
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }

        private static async Task<JobDetail> ExtractJobDetailAsync(IPage page)
        {
            var jd = new JobDetail();
            try
            {
                // Title: <h2 class="p1N2lc">
                jd.Title = (await page.Locator("h2.p1N2lc").InnerTextAsync()).Trim();

                // Location: <span class="pwO9Dc vo5qdf"> contains multiple <span class="r0wTof">
                var locationSpans = page.Locator("span.pwO9Dc.vo5qdf span.r0wTof");
                var locCount = await locationSpans.CountAsync();
                var locations = new List<string>();
                for (int j = 0; j < locCount; j++)
                {
                    var locText = (await locationSpans.Nth(j).InnerTextAsync()).Trim().TrimStart(';').Trim();
                    if (!string.IsNullOrEmpty(locText))
                        locations.Add(locText);
                }
                jd.Location = string.Join("; ", locations);

                // Department / Experience level: <span class="wVSTAb"> (e.g. "Advanced")
                var levelLocator = page.Locator("span.wVSTAb");
                if (await levelLocator.CountAsync() > 0)
                {
                    jd.Department = (await levelLocator.First.InnerTextAsync()).Trim();
                }

                // Apply URL: <a id="apply-action-button">
                var applyLink = page.Locator("a#apply-action-button");
                if (await applyLink.CountAsync() > 0)
                {
                    var href = await applyLink.GetAttributeAsync("href") ?? "";
                    jd.ApplyUrl = href.StartsWith("http") ? href : BaseUri + href.TrimStart('.', '/');
                }

                // Description (About the job): <div class="aG5W3">
                var aboutSection = page.Locator("div.aG5W3");
                if (await aboutSection.CountAsync() > 0)
                {
                    jd.Description = (await aboutSection.InnerTextAsync()).Trim();
                }

                // Responsibilities: <div class="BDNOWe">
                var respSection = page.Locator("div.BDNOWe");
                if (await respSection.CountAsync() > 0)
                {
                    jd.Responsibilities = (await respSection.InnerTextAsync())
                        .Replace("Responsibilities", "").Trim();
                }

                // Minimum qualifications (Requirements): content inside <div class="KwJkGe">
                // after the <h3> "Minimum qualifications:"
                var minQualHeader = page.Locator("h3:text('Minimum qualifications')");
                if (await minQualHeader.CountAsync() > 0)
                {
                    var minQualList = minQualHeader.Locator("~ ul").First;
                    if (await minQualList.CountAsync() > 0)
                    {
                        jd.Requirements = (await minQualList.InnerTextAsync()).Trim();
                    }
                }

                // Preferred qualifications (Achievements): content after <h3> "Preferred qualifications:"
                var prefQualHeader = page.Locator("h3:text('Preferred qualifications')");
                if (await prefQualHeader.CountAsync() > 0)
                {
                    var prefQualList = prefQualHeader.Locator("~ ul").First;
                    if (await prefQualList.CountAsync() > 0)
                    {
                        jd.Achievements = (await prefQualList.InnerTextAsync()).Trim();
                    }
                }

                // Compensation: extract from the description text if present
                if (!string.IsNullOrEmpty(jd.Description))
                {
                    var salaryIdx = jd.Description.IndexOf("base salary range", StringComparison.OrdinalIgnoreCase);
                    if (salaryIdx >= 0)
                    {
                        var salarySnippet = jd.Description[salaryIdx..];
                        var endIdx = salarySnippet.IndexOf('\n');
                        jd.Compensation = endIdx > 0
                            ? salarySnippet[..endIdx].Trim()
                            : salarySnippet.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting job detail: {ex.Message}");
            }
            await repoUtility.PoliteDelayAsync(100, 300);
            jd.Active = true;
            jd.StartDate = DateTime.UtcNow;

            return jd;
        }
    }
}
