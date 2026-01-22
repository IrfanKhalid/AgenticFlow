using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        // register CrawlerAgent from the agents project
        services.AddSingleton<AgentsAPI.Agents.CrawlerAgent>();
        services.AddSingleton<AgentsAPI.Agents.ICrawlerAgent>(sp => sp.GetRequiredService<AgentsAPI.Agents.CrawlerAgent>());
        services.AddHostedService(sp => sp.GetRequiredService<AgentsAPI.Agents.CrawlerAgent>());

        // register the cron background service that will run scheduled crawls
        services.AddHostedService<AgentsAPI.CronScheduler.CronBackgroundService>();
        // register the scraper project types
        services.AddSingleton<Microsoft.Playwright.IBrowser>(sp => {
            // create Playwright and browser once per process
            var playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
            var browser = playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions{Headless=true}).GetAwaiter().GetResult();
            return browser;
        });
    })
    .UseConsoleLifetime();

var host = builder.Build();
await host.RunAsync();
