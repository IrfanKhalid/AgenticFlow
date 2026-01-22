using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using BrowserTypeLaunchOptions = Microsoft.Playwright.BrowserTypeLaunchOptions;

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
        services.AddSingleton<IBrowser>(sp => {
            // initialize Playwright/browser in a hosted initializer to avoid blocking
            var playwright = Microsoft.Playwright.Playwright.CreateAsync().GetAwaiter().GetResult();
            var browser = playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions{Headless=true}).GetAwaiter().GetResult();
            return browser;
        });
    })
    .UseConsoleLifetime();

var host = builder.Build();
await host.RunAsync();
