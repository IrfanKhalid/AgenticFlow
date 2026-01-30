using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AgentsAPI.Agents
{
    public class CrawlerAgent : BackgroundService, ICrawlerAgent
    {
        private readonly Channel<(string Query, TaskCompletionSource<string> Tcs)> _queue;
        private readonly ILogger<CrawlerAgent> _logger;

        public CrawlerAgent(ILogger<CrawlerAgent> logger)
        {
            _logger = logger;
            _queue = Channel.CreateUnbounded<(string, TaskCompletionSource<string>)>();
        }

        public Task<string> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query is required", nameof(query));

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
            }

            var item = (Query: query, Tcs: tcs);
            if (!_queue.Writer.TryWrite(item))
            {
                tcs.TrySetException(new InvalidOperationException("Crawler queue is closed."));
            }

            return tcs.Task;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CrawlerAgent starting.");

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });

                var reader = _queue.Reader;

                while (await reader.WaitToReadAsync(stoppingToken))
                {
                    while (reader.TryRead(out var item))
                    {
                        try
                        {
                            _logger.LogInformation("Processing query: {Query}", item.Query);

                            var page = await browser.NewPageAsync();

                            await page.GotoAsync("https://www.google.com", new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 });
                            await page.FillAsync("textarea[name='q']", item.Query);
                            await page.PressAsync("textarea[name='q']", "Enter");
                            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                            var results = await page.QuerySelectorAllAsync("h3");
                            var titles = new List<string>();
                            foreach (var result in results)
                            {
                                try
                                {
                                    var text = await result.InnerTextAsync();
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        titles.Add(text);
                                    }
                                }
                                catch
                                {
                                }
                            }

                            await page.CloseAsync();

                            item.Tcs.TrySetResult(string.Join("\n", titles));
                        }
                        catch (OperationCanceledException oce)
                        {
                            item.Tcs.TrySetCanceled(oce.CancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing query '{Query}'", item.Query);
                            item.Tcs.TrySetException(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in CrawlerAgent ExecuteAsync.");
                while (_queue.Reader.TryRead(out var pending))
                {
                    pending.Tcs.TrySetException(ex);
                }
            }
            finally
            {
                _logger.LogInformation("CrawlerAgent stopping.");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _queue.Writer.Complete();
            return base.StopAsync(cancellationToken);
        }
    }
}