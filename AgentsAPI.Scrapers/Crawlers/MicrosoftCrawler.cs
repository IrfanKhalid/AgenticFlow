using AgentsAPI.Scrapers.Crawlers.Utility;
using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class MicrosoftCrawler
    {
        public static async Task<List<JobDetail>> CrawlMicrosoftAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync(
                    "https://apply.careers.microsoft.com/careers?query=&location=&start=0");

                //await page.GetByRole(AriaRole.Button, new() { Name = "Find jobs" }).ClickAsync();
                await repoUtility.PoliteDelayAsync(900, 3000);

                var baseUri = "https://apply.careers.microsoft.com";

                var ariaDisabled = await page.Locator("button[aria-label='Next jobs']").GetAttributeAsync("aria-disabled");
                while (ariaDisabled != "true")
                {
                    ariaDisabled = await page.Locator("button[aria-label='Next jobs']").GetAttributeAsync("aria-disabled");
                    var links = new List<string>();

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

                    var count = await page.Locator("div[data-test-id='job-listing'] a[href]:not([aria-roledescription='slide'])").CountAsync();

                    for (int i = 0; i < count; i++)
                    {
                        var jobLink = page.Locator("div[data-test-id='job-listing'] a[href]:not([aria-roledescription='slide'])")
                                    .Nth(i);
                        var href = await jobLink.GetAttributeAsync("href");
                        await jobLink.ScrollIntoViewIfNeededAsync();
                        await jobLink.ClickAsync();
                        await repoUtility.PoliteDelayAsync(300, 700);
                        try
                        {
                            var jd = new JobDetail();
                            await page.WaitForSelectorAsync(".detailContainer-2qNET");

                            var details = new Dictionary<string, string>();

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

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"exception:{ex}");
                        }
                    }
                    await page.Locator("button[aria-label='Next jobs']").ClickAsync();
                    await repoUtility.PoliteDelayAsync(500, 900);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"exception:{ex}");
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }
    }
}
