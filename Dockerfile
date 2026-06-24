# syntax=docker/dockerfile:1

# ---- Build stage ----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project files first so `dotnet restore` is cached across
# source-only changes. We restore the WebApi project (not the whole solution):
# it transitively pulls Domain/Application/Infrastructure, and deliberately
# excludes tests/LMS.Tests — the deploy image neither copies nor needs the test
# project, and restoring LMS.sln would fail looking for its csproj.
COPY LMS.Domain/LMS.Domain.csproj           LMS.Domain/
COPY LMS.Application/LMS.Application.csproj  LMS.Application/
COPY LMS.Infrastructure/LMS.Infrastructure.csproj LMS.Infrastructure/
COPY LMS.WebApi/LMS.WebApi.csproj           LMS.WebApi/
RUN dotnet restore LMS.WebApi/LMS.WebApi.csproj

# Copy the rest and publish the WebApi.
COPY . .
RUN dotnet publish LMS.WebApi/LMS.WebApi.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime stage --------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl is used by the compose healthcheck against /health.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container; Caddy proxies to it.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LMS.WebApi.dll"]
