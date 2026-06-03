# Build context muss der Repository-Root sein:
#   docker build -f samples/DRYL.Components.Demo/Dockerfile -t dryl/components .

# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Projektdateien zuerst kopieren (Layer-Caching für Restore)
COPY DRYL.Components/DRYL.Components.csproj DRYL.Components/
COPY samples/DRYL.Components.Demo/DRYL.Components.Demo.csproj samples/DRYL.Components.Demo/

RUN dotnet restore samples/DRYL.Components.Demo/DRYL.Components.Demo.csproj

# Restlichen Quellcode kopieren und publizieren
COPY DRYL.Components/ DRYL.Components/
COPY samples/DRYL.Components.Demo/ samples/DRYL.Components.Demo/

RUN dotnet publish samples/DRYL.Components.Demo/DRYL.Components.Demo.csproj \
    -c Release \
    -o /app/publish

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Nicht-root-Benutzer für Sicherheit
RUN useradd -m appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "DRYL.Components.Demo.dll"]
