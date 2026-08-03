FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HostelSite/*.csproj ./HostelSite/
RUN dotnet restore ./HostelSite/HostelSite.csproj

COPY HostelSite/ ./HostelSite/
WORKDIR /src/HostelSite
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HostelSite.dll"]
