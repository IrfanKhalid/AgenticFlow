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
var connectionString = builder.Configuration.GetConnectionString("Postgres") ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION") ?? "Host=localhost;Database=agentsdb;Username=postgres;Password=postgres";
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
