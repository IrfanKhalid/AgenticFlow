using AgentsAPI.Scrapers.Crawlers.Utility;
using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class MicrosoftCrawler
    {
        #region Public API

        public static async Task<List<JobDetail>> CrawlMicrosoftAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                #region Initial Navigation

                await page.GotoAsync(
                    "https://apply.careers.microsoft.com/careers?query=&location=&start=0");

                await repoUtility.PoliteDelayAsync(900, 3000);

                var baseUri = "https://apply.careers.microsoft.com";

                #endregion

                #region Main Paging Loop

                var ariaDisabled = await page.Locator("button[aria-label='Next jobs']").GetAttributeAsync("aria-disabled");
                while (ariaDisabled != "true")
                {
                    ariaDisabled = await page.Locator("button[aria-label='Next jobs']").GetAttributeAsync("aria-disabled");
                    var links = new List<string>();

                    #region Collect Job Links

                    var anchors = await page.QuerySelectorAllAsync(
                        "div[data-test-id='job-listing'] a[href]:not([aria-roledescription='slide'])"
                    );

                    foreach (var anchor in anchors)
                    {
                        var href = await anchor.GetAttributeAsync("href") ?? "";

                        if (string.IsNullOrWhiteSpace(href))
                            continue;
                        var absoluteUrl = baseUri + href;
                        links.Add(absoluteUrl);
                    }

                    await page.WaitForSelectorAsync("div[data-test-id='job-listing'] a[href]");
                    var count = await page.Locator("div[data-test-id='job-listing'] a[href]:not(.similarJobsCard-1CGdx)").CountAsync();

                    #endregion

                    #region Process Job Details

                    for (int i = 0; i < count; i++)
                    {
                        var jobLink = page.Locator("div[data-test-id='job-listing'] a[href]:not(.similarJobsCard-1CGdx)")
                                    .Nth(i);
                        var href = await jobLink.GetAttributeAsync("href");
                        await jobLink.ScrollIntoViewIfNeededAsync();
                        await jobLink.ClickAsync();
                        await repoUtility.PoliteDelayAsync(300, 700);

                        try
                        {
                            var jd = new JobDetail
                            {
                                EffectiveDate = DateTime.UtcNow,
                                CrawlerName = "Microsoft"
                            };
                            await page.WaitForSelectorAsync(".detailContainer-2qNET");

                            var containers = await page.QuerySelectorAllAsync(".detailContainer-2qNET");

                            foreach (var container in containers)
                            {
                                var labelElement = await container.QuerySelectorAsync(".detailLabel-2AsIg");
                                var valueElement = await container.QuerySelectorAsync(".detailValue-3NGwm");

                                var label = (await labelElement.InnerTextAsync()).Trim();
                                var value = (await valueElement.InnerTextAsync()).Trim();

                                if (label.Contains("Travel") || label.Contains("Work site"))
                                {
                                    jd.Location += " " + value;
                                }
                                else if (label.Contains("Profession") || label.Contains("Discipline") || label.Contains("Role type"))
                                {
                                    jd.Department += " " + value;
                                }
                                else if (label.Contains("Date posted"))
                                {
                                    jd.StartDate = DateTime.Parse(value);
                                }
                            }

                            jd.Description = await page.Locator(".container-3Gm1a").InnerTextAsync();
                            jd.Title = (await page.InnerTextAsync("h2.position-title-3TPtN")).Trim();
                            jd.ApplyUrl = baseUri + href;

                            await repoUtility.PoliteDelayAsync(300, 700);
                            results.Add(jd);
                            await repoUtility.FlushBatchIfNeededAsync(results, 500);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"exception:{ex}");
                        }
                    }

                    #endregion

                    #region Pagination

                    await page.Locator("button[aria-label='Next jobs']").ClickAsync();
                    await repoUtility.PoliteDelayAsync(500, 900);

                    #endregion
                }

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exception:{ex}");
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }

        #endregion
    }
}
