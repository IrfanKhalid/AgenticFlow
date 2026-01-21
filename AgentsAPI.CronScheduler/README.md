This project is a simple cron-based console scheduler.

- Uses `Cronos` to parse cron expressions.
- Uses `HtmlAgilityPack` to perform simple crawling of configured sites.

How to use:
- Edit `Program.cs` and set `CronExpression` and `sites` list or pass configuration.
- Build and run: `dotnet run --project AgentsAPI.CronScheduler/AgentsAPI.CronScheduler.csproj`

You can extend it to read from configuration, integrate Playwright, or post results to an API.