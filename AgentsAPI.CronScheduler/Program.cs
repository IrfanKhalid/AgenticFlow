using System;
using AgentsAPI.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
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
await ApplyMigrationsWithRetryAsync(maxAttempts: 10, delaySeconds: 5);
await host.RunAsync();

static async Task ApplyMigrationsWithRetryAsync(int maxAttempts, int delaySeconds)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            var connectionString = DbConnectionStringProvider.GetPostgres();
            var optionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            await using var db = new AgentsDbContext(optionsBuilder.Options);
            await db.Database.MigrateAsync();
            return;
        }
        catch when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    var finalConnectionString = DbConnectionStringProvider.GetPostgres();
    var finalOptionsBuilder = new DbContextOptionsBuilder<AgentsDbContext>();
    finalOptionsBuilder.UseNpgsql(finalConnectionString);
    await using var finalDb = new AgentsDbContext(finalOptionsBuilder.Options);
    await finalDb.Database.MigrateAsync();
}
