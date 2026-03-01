using AgentsAPI.Scrapers.Crawlers.Utility;
using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class AmazonCrawler
    {
        private const int NavTimeout = 30000;
        private const int ShortTimeout = 10000;
        private const int RetryCount = 2;

        public static async Task<List<JobDetail>> CrawlAmazonAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await NavigateWithRetryAsync(page,
                    "https://www.amazon.jobs/en/search?offset=0&result_limit=10&sort=relevant&business_category%5B%5D=amazon-web-services&cmpid=AS_OTAW200199B");

                await page.WaitForSelectorAsync("h3.job-title a.job-link",
                    new PageWaitForSelectorOptions { Timeout = NavTimeout });

                string currentUrl = page.Url;
                var nextSelector = "button.btn.circle.right[aria-label='Next page']";

                while (true)
                {
                    // Collect all job links from the current listing page
                    var links = new List<(string Title, string Url)>();
                    var jobLinks = page.Locator("h3.job-title a.job-link");
                    var seen = new HashSet<string>();
                    var count = await jobLinks.CountAsync();

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var link = jobLinks.Nth(i);
                            string href = await link.GetAttributeAsync("href",
                                new LocatorGetAttributeOptions { Timeout = ShortTimeout }) ?? "";
                            if (!string.IsNullOrEmpty(href) && seen.Add(href))
                            {
                                string title = (await link.InnerTextAsync(
                                    new LocatorInnerTextOptions { Timeout = ShortTimeout })).Trim();
                                string fullUrl = new Uri(new Uri(page.Url), href).ToString();
                                links.Add((title, fullUrl));
                            }
                        }
                        catch
                        {
                            // skip this link if we can't read it
                        }
                    }

                    // Visit each job detail page
                    foreach (var item in links)
                    {
                        try
                        {
                            var navigated = await NavigateWithRetryAsync(page, item.Url);
                            if (!navigated)
                                continue;

                            await repoUtility.PoliteDelayAsync(300, 700);

                            var jd = new JobDetail
                            {
                                Title = item.Title.Trim(),
                                ApplyUrl = item.Url
                            };

                            // Department
                            try
                            {
                                var categoryLink = page.Locator(".association.job-category-icon a").First;
                                var categoryAriaLabel = await categoryLink.GetAttributeAsync("aria-label",
                                    new LocatorGetAttributeOptions { Timeout = ShortTimeout });
                                jd.Department = categoryAriaLabel?.Replace("Job category ", "").Trim() ?? "";
                            }
                            catch
                            {
                                try
                                {
                                    jd.Department = await page.Locator(".association.job-category-icon li").First
                                        .InnerTextAsync(new LocatorInnerTextOptions { Timeout = 5000 });
                                }
                                catch { }
                            }

                            // Location
                            try
                            {
                                jd.Location = (await page.Locator(".association.location-icon li").First
                                    .InnerTextAsync(new LocatorInnerTextOptions { Timeout = ShortTimeout }))?.Trim() ?? "";
                            }
                            catch { }

                            // Description
                            try
                            {
                                jd.Description = await page.Locator("div.col-12.col-md-7.col-lg-8.col-xl-9")
                                    .InnerTextAsync(new LocatorInnerTextOptions { Timeout = ShortTimeout });
                            }
                            catch { }

                            // Basic qualifications
                            try
                            {
                                jd.Requirements = (await page.Locator(".section:has(h2:text('Basic Qualifications'))").First
                                    .TextContentAsync(new LocatorTextContentOptions { Timeout = ShortTimeout }))?
                                    .Replace("Basic Qualifications", "").Trim();
                            }
                            catch { }

                            // Preferred qualifications
                            try
                            {
                                jd.Responsibilities = (await page.Locator(".section:has(h2:text('Preferred Qualifications'))").First
                                    .TextContentAsync(new LocatorTextContentOptions { Timeout = ShortTimeout }))?
                                    .Replace("Preferred Qualifications", "").Trim();
                            }
                            catch { }

                            results.Add(jd);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"AmazonCrawler: failed on {item.Url}: {ex.Message}");
                        }
                    }

                    // Navigate back to the listing page
                    var backOk = await NavigateWithRetryAsync(page, currentUrl);
                    if (!backOk)
                        break;

                    await page.WaitForSelectorAsync("h3.job-title a.job-link",
                        new PageWaitForSelectorOptions { Timeout = NavTimeout });
                    await repoUtility.PoliteDelayAsync(200, 500);

                    // Check if next page button exists and is enabled
                    var nextButton = page.Locator(nextSelector);
                    if (await nextButton.CountAsync() == 0)
                        break;

                    try
                    {
                        var isDisabled = await nextButton.IsDisabledAsync(
                            new LocatorIsDisabledOptions { Timeout = ShortTimeout });
                        if (isDisabled)
                        {
                            Console.WriteLine("Next button is disabled. Stopping.");
                            break;
                        }

                        await nextButton.ClickAsync(new LocatorClickOptions { Timeout = ShortTimeout });
                        await page.WaitForSelectorAsync("h3.job-title a.job-link",
                            new PageWaitForSelectorOptions { Timeout = NavTimeout });
                        currentUrl = page.Url;
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("AmazonCrawler: pagination timed out, stopping.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AmazonCrawler error: {ex.Message}");
            }
            finally
            {
                await page.CloseAsync();
            }

            return results;
        }

        /// <summary>
        /// Navigates to a URL with DOMContentLoaded wait and retry logic.
        /// Returns true if navigation succeeded.
        /// </summary>
        private static async Task<bool> NavigateWithRetryAsync(IPage page, string url)
        {
            for (int attempt = 0; attempt <= RetryCount; attempt++)
            {
                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = NavTimeout
                    });
                    return true;
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"AmazonCrawler: nav timeout for {url} (attempt {attempt + 1}/{RetryCount + 1})");
                    if (attempt == RetryCount)
                        return false;

                    await repoUtility.PoliteDelayAsync(1000, 2000);
                }
                catch (PlaywrightException ex) when (ex.Message.Contains("net::ERR_"))
                {
                    Console.WriteLine($"AmazonCrawler: network error for {url}: {ex.Message}");
                    if (attempt == RetryCount)
                        return false;

                    await repoUtility.PoliteDelayAsync(2000, 4000);
                }
            }
            return false;
        }
    }
}
