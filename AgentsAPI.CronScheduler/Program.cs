using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
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
    })
    .Build();

await host.RunAsync();
