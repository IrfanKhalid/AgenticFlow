# Copilot instructions for AgentsAPI

## Big picture (read this first)
- This solution is a .NET 9 multi-project backend split by responsibility: `AgentsAPI.WebAPI` (HTTP), `AgentsAPI.BusinessLogic` (service layer), `AgentsAPI.DataAccess` (EF Core + repositories), `AgentsAPI.Scrapers` (site-specific crawlers), `AgentsAPI.CronScheduler` (scheduled crawler host), `AgentsAPI.Agents` (queue-based search agent), and `AgentsAPI.Shared` (cross-project models).
- `AgentsAPI.WebAPI/Program.cs` wires DI and also hosts `CrawlerAgent` as a singleton `BackgroundService`; startup runs EF migrations with retry.
- `AgentsAPI.CronScheduler/Program.cs` is a separate host process: it runs migrations, then executes `CronBackgroundService` which schedules crawlers from DB state.

## Core runtime/data flow
- Scheduled scraping flow: `CronBackgroundService` -> `CrawlerRegistry` name lookup -> `AgentsAPI.Scrapers.Crawlers.*` static crawler -> `JobRepository.AddOrUpdateAsync` -> `JobDetails` table.
- Scheduler concurrency/locking is DB-driven (not in-memory): `CronBackgroundService` atomically sets `CronCrawler.IsRunning` via `ExecuteUpdateAsync` and resets stale flags on startup.
- Scheduler observability is persisted: every run writes `CrawlerRuns` and `CrawlerLogs` in `AgentsDbContext`.
- API search flow: `SearchController` -> `SearchService` -> `ICrawlerAgent.SearchAsync`; `CrawlerAgent` uses an internal `Channel<(query, tcs)>` queue and Playwright-based Google scraping.

## Project conventions to follow
- Keep layering strict: controllers call business interfaces (`IItemService`, `ISearchService`), business layer calls repositories/agents, DB access lives in `AgentsAPI.DataAccess`.
- Shared entities live in `AgentsAPI.Shared/Models` and are reused across hosts; avoid duplicating DTO/entity shapes in project-local folders unless API-specific.
- New scheduled crawler must be registered in 3 places:
  1) add crawler implementation in `AgentsAPI.Scrapers/Crawlers/`,
  2) map its name in `AgentsAPI.CronScheduler/CrawlerRegistry.cs`,
  3) seed/insert matching `CronCrawler.CrawlerName` value in DB (see seed data in `AgentsDbContext`).
- Keep `CrawlerName` strings consistent across scrapers, registry, and `CronCrawlers` rows (case-insensitive lookup exists, but semantic mismatch still breaks scheduling).
- Connection strings are resolved via `DbConnectionStringProvider.GetPostgres(...)` with fallback order: explicit configured string -> `ConnectionStrings__Postgres` -> `POSTGRES_CONNECTION` -> localhost default.

## Developer workflows (repo-specific)
- Build all projects: `dotnet build AgentsAPI.sln`
- Run Web API: `dotnet run --project AgentsAPI.WebAPI/AgentsAPI.WebAPI.csproj`
- Run scheduler: `dotnet run --project AgentsAPI.CronScheduler/AgentsAPI.CronScheduler.csproj`
- Docker local stack (Postgres + Web API + scheduler): `docker compose up --build`
- API migrations are applied automatically on startup in both hosts; manual migration script also exists at `AgentsAPI.DataAccess/migrations.sql`.
- There is currently no test project in this solution (`*.csproj` contains no test SDK references), so validate by targeted host runs and endpoint checks.

## Integration/dependency notes
- PostgreSQL is the only configured datastore (`Npgsql.EntityFrameworkCore.PostgreSQL`), with cron state stored in `CronCrawlers` and job data in `JobDetails`.
- Playwright is a critical runtime dependency for both `AgentsAPI.Agents` and scheduled crawlers; `AgentsAPI.CronScheduler/Dockerfile` installs Chromium and OS deps.
- `docker-compose.yml` expects env vars and defaults aligned with `DbConnectionStringProvider` conventions; preserve this mapping when changing config keys.

## Practical editing guidance for agents
- Prefer small, project-local changes; avoid cross-layer shortcuts (for example, do not call EF directly from controllers).
- When changing crawler persistence behavior, inspect both `JobRepository` normalization rules and `AgentsDbContext` column constraints/computed fields (e.g., `JobDetail.ContentHash` computed SQL).
- If adding high-volume crawler output, keep batch-save pattern compatible with `InsertJobsByBatch` / `repoUtility.FlushBatchIfNeededAsync` to avoid huge single writes.
