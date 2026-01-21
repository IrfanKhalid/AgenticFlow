using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Playwright;
using System.Net.Http;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace AgentsAPI.CronScheduler
{
    public static class ScrappingJobs
    {
        // Reads the CSV from the Shared project and returns the list of websites (Website column)
        public static async IAsyncEnumerable<Task> ReadJobSitesFromShared(string solutionRoot)
        {
            // Path relative to solution root
            var relativePath = Path.Combine("AgentsAPI.Shared", "Files", "Job Sites.csv");
            var fullPath = "C:\\Irfan\\Work\\Deep Learning\\Agentic Learning\\AgenticApi\\AgentsAPI.Shared\\Files\\Job Sites.csv"; //Path.Combine(solutionRoot, relativePath);

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
                    var crawlTask = CrawlSitesAsync(site);
                    await crawlTask;
                    yield return crawlTask;
                }
            }
        }

        // Opens Playwright browser and navigates to each site sequentially
        public static async Task CrawlSitesAsync(string site)
        {
            // normalize site url
            if (string.IsNullOrWhiteSpace(site))
                return;

            if (!Regex.IsMatch(site, "^https?://", RegexOptions.IgnoreCase))
            {
                site = "https://" + site.TrimStart('/');
            }

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });

            var context = await browser.NewContextAsync();
            try
            {
                // 1. Try sitemap.xml
                var sitemapUrls = new List<string>();
                try
                {
                    using var http = new HttpClient();
                    var sitemapUrl = new Uri(new Uri(site), "sitemap.xml").ToString();
                    var sitemapContent = await http.GetStringAsync(sitemapUrl);

                    var doc = XDocument.Parse(sitemapContent);
                    var locs = doc.Descendants().Where(e => string.Equals(e.Name.LocalName, "loc", StringComparison.OrdinalIgnoreCase))
                                 .Select(e => e.Value.Trim())
                                 .Where(v => !string.IsNullOrWhiteSpace(v))
                                 .Distinct()
                                 .ToList();

                    // limit to a reasonable number to avoid long runs
                    sitemapUrls.AddRange(locs.Take(100));
                }
                catch
                {
                    // ignore sitemap fetch/parse errors
                }

                // If no sitemap entries, fallback to crawling the root page for links
                if (!sitemapUrls.Any())
                {
                    sitemapUrls.Add(site);
                }

                var discoveredLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // keywords to identify career pages
                var careerKeywords = new[] { "career", "careers", "job", "jobs", "join", "opportunity", "vacancy", "vacancies" };

                // inspect each sitemap URL and collect links
                var limitPerSite = 50;
                foreach (var url in sitemapUrls)
                {
                    try
                    {
                        var page = await context.NewPageAsync();
                        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                        var anchors = await page.QuerySelectorAllAsync("a[href]");
                        var count = 0;
                        foreach (var a in anchors)
                        {
                            if (count++ > limitPerSite) break;
                            try
                            {
                                var href = await a.GetAttributeAsync("href");
                                if (string.IsNullOrWhiteSpace(href)) continue;

                                // normalize relative urls
                                if (Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var hrefUri))
                                {
                                    string abs;
                                    if (!hrefUri.IsAbsoluteUri)
                                    {
                                        abs = new Uri(new Uri(url), href).ToString();
                                    }
                                    else
                                    {
                                        abs = hrefUri.ToString();
                                    }

                                    // only keep http(s)
                                    if (abs.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || abs.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                    {
                                        discoveredLinks.Add(abs);
                                    }
                                }
                            }
                            catch
                            {
                                // ignore individual anchor errors
                            }
                        }

                        await page.CloseAsync();
                    }
                    catch(Exception ex)
                    {
                        // ignore individual page errors
                    }
                }

                // find career links among discovered links and sitemap urls themselves
                var careerLinks = discoveredLinks.Where(u => careerKeywords.Any(k => u.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                careerLinks.AddRange(sitemapUrls.Where(u => careerKeywords.Any(k => u.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0) && !careerLinks.Contains(u)));

                // dedupe
                careerLinks = careerLinks.Distinct(StringComparer.OrdinalIgnoreCase).Take(50).ToList();

                // Crawl career pages and try to extract job description sections
                foreach (var careerUrl in careerLinks)
                {
                    try
                    {
                        var page = await context.NewPageAsync();
                        await page.GotoAsync(careerUrl, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                        string description = string.Empty;

                        // 1) meta description
                        try
                        {
                            var meta = await page.QuerySelectorAsync("meta[name=description]");
                            if (meta != null)
                            {
                                var content = await meta.GetAttributeAsync("content");
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    description = content.Trim();
                                }
                            }
                        }
                        catch { }

                        // 2) Look for headings containing 'description' and take following sibling text
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            try
                            {
                                var headings = await page.QuerySelectorAllAsync("h1,h2,h3,h4");
                                foreach (var h in headings)
                                {
                                    var txt = (await h.InnerTextAsync()) ?? string.Empty;
                                    if (txt.IndexOf("description", StringComparison.OrdinalIgnoreCase) >= 0 || txt.IndexOf("job description", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        // try to get next sibling text
                                        try
                                        {
                                            var siblingText = await h.EvaluateAsync<string>("(el) => { var s = el.nextElementSibling; return s ? s.innerText : ''; }");
                                            if (!string.IsNullOrWhiteSpace(siblingText))
                                            {
                                                description = siblingText.Trim();
                                                break;
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            catch { }
                        }

                        // 3) fallback: pick the longest paragraph (<p>) text
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            try
                            {
                                var paras = await page.QuerySelectorAllAsync("p");
                                string best = string.Empty;
                                foreach (var p in paras)
                                {
                                    try
                                    {
                                        var t = await p.InnerTextAsync();
                                        if (!string.IsNullOrWhiteSpace(t) && t.Length > best.Length)
                                            best = t.Trim();
                                    }
                                    catch { }
                                }

                                description = best;
                            }
                            catch { }
                        }

                        // 4) last resort: take full page innerText and trim
                        if (string.IsNullOrWhiteSpace(description))
                        {
                            try
                            {
                                var body = await page.EvaluateAsync<string>("() => document.body ? document.body.innerText : ''");
                                if (!string.IsNullOrWhiteSpace(body))
                                    description = body.Trim();
                            }
                            catch { }
                        }

                        // output a trimmed excerpt for now
                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            var excerpt = description.Length > 1000 ? description.Substring(0, 1000) + "..." : description;
                            Console.WriteLine($"[Crawler] {careerUrl} -> {excerpt}");
                        }
                        else
                        {
                            Console.WriteLine($"[Crawler] {careerUrl} -> (no description found)");
                        }

                        await page.CloseAsync();
                    }
                    catch
                    {
                        // ignore individual career page errors
                    }
                }
            }
            catch
            {
                // swallow overall errors
            }
            finally
            {
                await context.CloseAsync();
                await browser.CloseAsync();
            }
        }
    }
}
