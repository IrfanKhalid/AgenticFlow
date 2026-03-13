using AgentsAPI.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddScoped<AgentsAPI.DataAccess.Repositories.IItemRepository, AgentsAPI.DataAccess.Repositories.ItemRepository>();
builder.Services.AddScoped<AgentsAPI.BusinessLogic.Services.IItemService, AgentsAPI.BusinessLogic.Services.ItemService>();
builder.Services.AddScoped<AgentsAPI.BusinessLogic.Services.ISearchService, AgentsAPI.BusinessLogic.Services.SearchService>();

// Configure Postgres DbContext
var connectionString = AgentsAPI.DataAccess.Models.DbConnectionStringProvider.GetPostgres(
    builder.Configuration.GetConnectionString("Postgres"));
builder.Services.AddDbContext<AgentsDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register job repository
builder.Services.AddScoped<AgentsAPI.DataAccess.Repositories.JobRepository>();

// Register CrawlerAgent as singleton and host it
builder.Services.AddSingleton<AgentsAPI.Agents.CrawlerAgent>();
builder.Services.AddSingleton<AgentsAPI.Agents.ICrawlerAgent>(sp => sp.GetRequiredService<AgentsAPI.Agents.CrawlerAgent>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<AgentsAPI.Agents.CrawlerAgent>());

var app = builder.Build();

// Apply pending EF Core migrations on startup (with retry for container cold start)
await ApplyMigrationsWithRetryAsync(app.Services, maxAttempts: 10, delaySeconds: 5);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

static async Task ApplyMigrationsWithRetryAsync(IServiceProvider services, int maxAttempts, int delaySeconds)
{
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigration");
            var db = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("No pending EF migrations.");
                return;
            }

            logger.LogInformation("Applying {Count} pending EF migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
            logger.LogInformation("EF migrations applied successfully.");
            return;
        }
        catch when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }

    using var finalScope = services.CreateScope();
    var finalDb = finalScope.ServiceProvider.GetRequiredService<AgentsDbContext>();
    await finalDb.Database.MigrateAsync();
}
