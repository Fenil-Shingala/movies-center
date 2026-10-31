# ==============================================================================
# Multi-stage Dockerfile for MoviesCenter Backend (ASP.NET Core 10 Web API)
# ==============================================================================

# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Stage 2: SDK build & dependency restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimal layer caching
COPY ["src/MoviesCenter.Domain/MoviesCenter.Domain.csproj", "src/MoviesCenter.Domain/"]
COPY ["src/MoviesCenter.Application/MoviesCenter.Application.csproj", "src/MoviesCenter.Application/"]
COPY ["src/MoviesCenter.Infrastructure/MoviesCenter.Infrastructure.csproj", "src/MoviesCenter.Infrastructure/"]
COPY ["src/MoviesCenter.Api/MoviesCenter.Api.csproj", "src/MoviesCenter.Api/"]

# Restore packages
RUN dotnet restore "src/MoviesCenter.Api/MoviesCenter.Api.csproj"

# Copy the rest of the backend source files
COPY . .

# Build the API project
WORKDIR "/src/src/MoviesCenter.Api"
RUN dotnet build "MoviesCenter.Api.csproj" -c Release -o /app/build --no-restore

# Stage 3: Publish binaries
FROM build AS publish
RUN dotnet publish "MoviesCenter.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Stage 4: Production final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install curl for container health check
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID

# Container healthcheck
HEALTHCHECK --interval=60s --timeout=5s --start-period=20s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MoviesCenter.Api.dll"]
