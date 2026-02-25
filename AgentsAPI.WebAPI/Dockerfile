# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files for restore
COPY AgentsAPI.WebAPI/AgentsAPI.WebAPI.csproj             AgentsAPI.WebAPI/
COPY AgentsAPI.BusinessLogic/AgentsAPI.BusinessLogic.csproj AgentsAPI.BusinessLogic/
COPY AgentsAPI.DataAccess/AgentsAPI.DataAccess.csproj     AgentsAPI.DataAccess/
COPY AgentsAPI.Shared/AgentsAPI.Shared.csproj             AgentsAPI.Shared/
COPY AgentsAPI.Agents/AgentsAPI.Agents.csproj             AgentsAPI.Agents/
COPY AgentsAPI.Scrapers/AgentsAPI.Scrapers.csproj         AgentsAPI.Scrapers/

RUN dotnet restore AgentsAPI.WebAPI/AgentsAPI.WebAPI.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish AgentsAPI.WebAPI/AgentsAPI.WebAPI.csproj -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AgentsAPI.WebAPI.dll"]
