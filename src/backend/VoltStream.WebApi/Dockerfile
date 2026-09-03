# Angular SPA -> wwwroot
FROM node:24-alpine AS web
WORKDIR /src
COPY src/web/VoltStream.Web/package.json src/web/VoltStream.Web/package-lock.json src/web/VoltStream.Web/
RUN cd src/web/VoltStream.Web && npm ci
COPY src/web/VoltStream.Web/ src/web/VoltStream.Web/
RUN cd src/web/VoltStream.Web && npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY VoltStream.sln .
COPY src/backend/VoltStream.WebApi/VoltStream.WebApi.csproj src/backend/VoltStream.WebApi/
COPY src/backend/VoltStream.Application/VoltStream.Application.csproj src/backend/VoltStream.Application/
COPY src/backend/VoltStream.Domain/VoltStream.Domain.csproj src/backend/VoltStream.Domain/
COPY src/backend/VoltStream.Infrastructure/VoltStream.Infrastructure.csproj src/backend/VoltStream.Infrastructure/
RUN dotnet restore src/backend/VoltStream.WebApi/VoltStream.WebApi.csproj
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/VoltStream.WebApi/VoltStream.WebApi.csproj \
    -c Release -o /app/publish -p:BuildWeb=false -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=web /src/src/backend/VoltStream.WebApi/wwwroot ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -fsS http://localhost:8080/api/health || exit 1
ENTRYPOINT ["dotnet", "VoltStream.WebApi.dll"]
