using AgentsAPI.Shared.Models;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace AgentsAPI.Scrapers.Crawlers
{
    public static class AmazonCrawler
    {
        public static async Task<List<JobDetail>> CrawlAmazonAsync(IBrowserContext browser)
        {
            if (browser == null) throw new ArgumentNullException(nameof(browser));

            var results = new List<JobDetail>();
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync("https://www.amazon.jobs/content/en/teams/amazon-web-services");

                page.WaitForTimeoutAsync(1000);
                //await page.GetByRole(AriaRole.Link, new() { Name = "View open roles" }).ClickAsync();
                await page.GotoAsync("https://www.amazon.jobs/en/search?offset=0&result_limit=10&sort=relevant&business_category%5B%5D=amazon-web-services&cmpid=AS_OTAW200199B");

                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);


                string currentUrl = page.Url;

                var ariaDisabled = "button.btn.circle.right[aria-label='Next page']";
                while (true)
                {
                    var links = new List<(string Title, string Url)>();

                    var jobLinks = page.Locator("h3.job-title a.job-link");

                    var seen = new HashSet<string>();
                    var check = await jobLinks.CountAsync();
                    for (int i = 0; i < await jobLinks.CountAsync(); i++)
                    {
                        var link = jobLinks.Nth(i);
                        string href = await link.GetAttributeAsync("href");

                        if (seen.Add(href))
                        {
                            string title = (await link.InnerTextAsync()).Trim();
                            string fullUrl = new Uri(new Uri(page.Url), href).ToString();
                            links.Add((title, fullUrl));

                        }
                    }




                    foreach (var item in links)
                    {
                        await page.GotoAsync(item.Url);
                        await page.WaitForTimeoutAsync(1000);

                        var jd = new JobDetail();
                        try
                        {
                            jd.Title = item.Title.Trim();
                            jd.ApplyUrl = item.Url;
                            try
                            {
                                var categoryLink = page.Locator(".association.job-category-icon a").First;
                                var categoryAriaLabel = await categoryLink.GetAttributeAsync("aria-label");
                                jd.Department = categoryAriaLabel?.Replace("Job category ", "").Trim();

                            }
                            catch (Exception ex)
                            {
                                var categoryLink = page.Locator(".association.job-category-icon li").First;
                                
                                jd.Department = await categoryLink.InnerTextAsync();
                            }
                            var locationText = await page.Locator(".association.location-icon li").First.InnerTextAsync();
                            jd.Location = locationText?.Trim();


                            var content = page.Locator("div.col-12.col-md-7.col-lg-8.col-xl-9");

                            jd.Description = await content.InnerTextAsync();

                            // Get basic qualifications
                            var basicQualSection =  page.Locator(".section:has(h2:text('Basic Qualifications'))").First;
                            if (basicQualSection != null)
                            {
                               jd.Requirements = (await basicQualSection.TextContentAsync())?
                                    .Replace("Basic Qualifications", "").Trim();
                            }

                            // Get preferred qualifications
                            var preferredQualSection = page.Locator(".section:has(h2:text('Preferred Qualifications'))").First;
                            if (preferredQualSection != null)
                            {
                                jd.Responsibilities = (await preferredQualSection.TextContentAsync())?
                                    .Replace("Preferred Qualifications", "").Trim();
                            }


                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                            results.Add(jd);
                        }
                        catch (Exception ex)
                        {
                            // ignore single job failures
                        }
                    }
                    await page.GotoAsync(currentUrl);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    var isDisabled = await page.IsDisabledAsync(ariaDisabled);
                    if (isDisabled)
                    {
                        Console.WriteLine("Next button is disabled. Stopping.");
                        break;
                    }

                    // Click the button
                    await page.ClickAsync(ariaDisabled);
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    currentUrl = page.Url;
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
