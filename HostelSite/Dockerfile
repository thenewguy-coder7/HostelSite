# ── BUILD STAGE ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file first (better Docker layer caching)
COPY HostelSite/*.csproj ./HostelSite/
RUN dotnet restore ./HostelSite/HostelSite.csproj

# Copy everything else and build
COPY HostelSite/ ./HostelSite/
WORKDIR /src/HostelSite
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── RUNTIME STAGE ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Render provides the PORT env variable — ASP.NET Core must listen on it
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HostelSite.dll"]
