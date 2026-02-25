# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files for restore
COPY AgentsAPI.CronScheduler/AgentsAPI.CronScheduler.csproj AgentsAPI.CronScheduler/
COPY AgentsAPI.Scrapers/AgentsAPI.Scrapers.csproj           AgentsAPI.Scrapers/
COPY AgentsAPI.DataAccess/AgentsAPI.DataAccess.csproj       AgentsAPI.DataAccess/
COPY AgentsAPI.Shared/AgentsAPI.Shared.csproj               AgentsAPI.Shared/
COPY AgentsAPI.Agents/AgentsAPI.Agents.csproj               AgentsAPI.Agents/
COPY AgentsAPI.BusinessLogic/AgentsAPI.BusinessLogic.csproj AgentsAPI.BusinessLogic/

RUN dotnet restore AgentsAPI.CronScheduler/AgentsAPI.CronScheduler.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish AgentsAPI.CronScheduler/AgentsAPI.CronScheduler.csproj -c Release -o /app/publish --no-restore

# Install Playwright browsers during build (uses the SDK + node bundled with Playwright)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN /app/publish/.playwright/node/linux-x64/node /app/publish/.playwright/package/lib/cli/cli.js install --with-deps chromium

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy Chromium browser binaries installed during build
COPY --from=build /ms-playwright /ms-playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# Install only the OS-level dependencies that Chromium needs at runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
    libdbus-1-3 libxkbcommon0 libatspi2.0-0 libxcomposite1 libxdamage1 \
    libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2 \
    fonts-liberation fonts-noto-color-emoji xdg-utils ca-certificates && \
    rm -rf /var/lib/apt/lists/*

# Copy the published app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AgentsAPI.CronScheduler.dll"]
