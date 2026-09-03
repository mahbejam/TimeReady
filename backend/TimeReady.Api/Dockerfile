FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TimeReady.Api/TimeReady.Api.csproj TimeReady.Api/
RUN dotnet restore TimeReady.Api/TimeReady.Api.csproj

COPY TimeReady.Api/ TimeReady.Api/
RUN dotnet publish TimeReady.Api/TimeReady.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# curl is only here so the container can answer Docker's health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Serilog writes a rolling file next to the application.
RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app/logs
USER $APP_UID

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TimeReady.Api.dll"]
