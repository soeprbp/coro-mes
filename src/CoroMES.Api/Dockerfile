# Build only the API and its referenced projects. The context allowlist excludes
# local databases, credentials, generated files, and unrelated untracked tools.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ src/
COPY modules/ modules/
COPY integration/ integration/
COPY industrial/ industrial/
RUN dotnet restore src/CoroMES.Api/CoroMES.Api.csproj
RUN dotnet publish src/CoroMES.Api/CoroMES.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p /app/data && chown -R "$APP_UID:$APP_UID" /app/data
COPY --from=build /app/publish ./
COPY web/ ./web/
ENV ASPNETCORE_URLS=http://+:5000
ENV CoroMES__SqlitePath=/app/data/coromes.db
USER $APP_UID
EXPOSE 5000
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail --silent http://127.0.0.1:5000/health > /dev/null || exit 1
ENTRYPOINT ["dotnet", "CoroMES.Api.dll"]
