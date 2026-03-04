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
        #region Constants

        private const string BaseUri = "https://www.google.com/about/careers/applications/";

        #endregion

        #region Public API

        public static async Task<List<JobDetail>> CrawlGoogleAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                #region Initial Navigation

                await page.GotoAsync("https://www.google.com/about/careers/applications/jobs/results");
                await page.Locator("div.VfPpkd-dgl2Hf-ppHlrf-sM5MNb >> a[href*='jobs/results']").First.ClickAsync();
                await repoUtility.PoliteDelayAsync(200, 600);

                #endregion

                while (true)
                {
                    #region Collect Listing Items

                    var jobItems = page.Locator("li.zE6MFb");
                    var count = await jobItems.CountAsync();

                    #endregion

                    #region Process Listing Items

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var item = jobItems.Nth(i);
                            var anchor = item.Locator("a.Si6A0c");

                            await repoUtility.PoliteDelayAsync(300, 700);
                            await anchor.ScrollIntoViewIfNeededAsync();
                            await anchor.ClickAsync();

                            await page.WaitForSelectorAsync("h2.p1N2lc", new PageWaitForSelectorOptions { Timeout = 30000 });

                            var dataId = await item.GetAttributeAsync("data-id") ?? "";
                            await page.WaitForSelectorAsync($"div.DkhPwc[data-id='{dataId}']",
                                new PageWaitForSelectorOptions { Timeout = 15000 });

                            var jd = await ExtractJobDetailAsync(page);
                            if (!string.IsNullOrEmpty(jd.Title))
                            {
                                results.Add(jd);
                                await repoUtility.FlushBatchIfNeededAsync(results, 500);
                            }
                        }
                            catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing job item {i}: {ex.Message}");
                        }
                    }

                    #endregion

                    #region Pagination

                    var nextLink = page.Locator("a[aria-label='Go to next page']").Last;
                    if (await nextLink.CountAsync() == 0)
                        break;

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

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }

        #endregion

        #region Private Helpers

        private static async Task<JobDetail> ExtractJobDetailAsync(IPage page)
        {
            var jd = new JobDetail();

            try
            {
                #region Core Fields

                jd.Title = (await page.Locator("h2.p1N2lc").InnerTextAsync()).Trim();

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

                var levelLocator = page.Locator("span.wVSTAb");
                if (await levelLocator.CountAsync() > 0)
                {
                    jd.Department = (await levelLocator.First.InnerTextAsync()).Trim();
                }

                #endregion

                #region Apply URL

                var applyLink = page.Locator("a#apply-action-button");
                if (await applyLink.CountAsync() > 0)
                {
                    var href = await applyLink.GetAttributeAsync("href") ?? "";
                    jd.ApplyUrl = href.StartsWith("http") ? href : BaseUri + href.TrimStart('.', '/');
                }

                #endregion

                #region Description and Sections

                var aboutSection = page.Locator("div.aG5W3");
                if (await aboutSection.CountAsync() > 0)
                {
                    jd.Description = (await aboutSection.InnerTextAsync()).Trim();
                }

                var respSection = page.Locator("div.BDNOWe");
                if (await respSection.CountAsync() > 0)
                {
                    jd.Responsibilities = (await respSection.InnerTextAsync())
                        .Replace("Responsibilities", "").Trim();
                }

                var minQualHeader = page.Locator("h3:text('Minimum qualifications')");
                if (await minQualHeader.CountAsync() > 0)
                {
                    var minQualList = minQualHeader.Locator("~ ul").First;
                    if (await minQualList.CountAsync() > 0)
                    {
                        jd.Requirements = (await minQualList.InnerTextAsync()).Trim();
                    }
                }

                var prefQualHeader = page.Locator("h3:text('Preferred qualifications')");
                if (await prefQualHeader.CountAsync() > 0)
                {
                    var prefQualList = prefQualHeader.Locator("~ ul").First;
                    if (await prefQualList.CountAsync() > 0)
                    {
                        jd.Achievements = (await prefQualList.InnerTextAsync()).Trim();
                    }
                }

                #endregion

                #region Compensation

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

                #endregion
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

        #endregion
    }
}
