# Portable Linux build for the Blazor application. Build context is repository root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/CoroMES.Web/CoroMES.Web.csproj
RUN dotnet publish src/CoroMES.Web/CoroMES.Web.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
RUN mkdir -p /app/data/keys && chown -R app:app /app/data
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:8080/health > /dev/null || exit 1
ENTRYPOINT ["dotnet", "CoroMES.Web.dll"]
