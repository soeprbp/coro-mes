# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["CoroMES.sln", "./"]
COPY ["src/CoroMES.Api/CoroMES.Api.csproj", "src/CoroMES.Api/"]
COPY ["src/CoroMES.Core/CoroMES.Core.csproj", "src/CoroMES.Core/"]
COPY ["src/CoroMES.Application/CoroMES.Application.csproj", "src/CoroMES.Application/"]
COPY ["src/CoroMES.Infrastructure/CoroMES.Infrastructure.csproj", "src/CoroMES.Infrastructure/"]
COPY ["src/CoroMES.Reporting/CoroMES.Reporting.csproj", "src/CoroMES.Reporting/"]
COPY ["modules/CoroMES.Production/CoroMES.Production.csproj", "modules/CoroMES.Production/"]
COPY ["modules/CoroMES.Quality/CoroMES.Quality.csproj", "modules/CoroMES.Quality/"]
COPY ["modules/CoroMES.Inventory/CoroMES.Inventory.csproj", "modules/CoroMES.Inventory/"]
COPY ["modules/CoroMES.Equipment/CoroMES.Equipment.csproj", "modules/CoroMES.Equipment/"]
COPY ["modules/CoroMES.Workforce/CoroMES.Workforce.csproj", "modules/CoroMES.Workforce/"]
COPY ["integration/CoroMES.Integration.TrueCommerce/CoroMES.Integration.TrueCommerce.csproj", "integration/CoroMES.Integration.TrueCommerce/"]
COPY ["integration/CoroMES.Integration.Upkeep/CoroMES.Integration.Upkeep.csproj", "integration/CoroMES.Integration.Upkeep/"]
COPY ["integration/CoroMES.Integration.IIoT/CoroMES.Integration.IIoT.csproj", "integration/CoroMES.Integration.IIoT/"]
COPY ["industrial/CoroMES.Industrial.Mqtt/CoroMES.Industrial.Mqtt.csproj", "industrial/CoroMES.Industrial.Mqtt/"]
COPY ["industrial/CoroMES.Industrial.OpcUa/CoroMES.Industrial.OpcUa.csproj", "industrial/CoroMES.Industrial.OpcUa/"]
COPY ["industrial/CoroMES.Industrial.EthernetIp/CoroMES.Industrial.EthernetIp.csproj", "industrial/CoroMES.Industrial.EthernetIp/"]
COPY ["industrial/CoroMES.Industrial.i3X/CoroMES.Industrial.i3X.csproj", "industrial/CoroMES.Industrial.i3X/"]

# Restore packages
RUN dotnet restore "CoroMES.sln"

# Copy all source code
COPY . .

# Build the application
RUN dotnet build "CoroMES.sln" -c Release -o /app/build

# Publish the API
RUN dotnet publish "src/CoroMES.Api/CoroMES.Api.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install timezone data and curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends tzdata curl && rm -rf /var/lib/apt/lists/*

# Set timezone
ENV TZ=America/Indiana/Indianapolis

# Copy published files
COPY --from=build /app/publish .
COPY --from=build /src/web ./web

# Run as root for now
USER root

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "CoroMES.Api.dll"]
