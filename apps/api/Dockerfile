# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution metadata and centralized package config first for layer caching
COPY NexusFlow.sln global.json Directory.Build.props Directory.Packages.props ./
COPY NexusFlow.Api/NexusFlow.Api.csproj NexusFlow.Api/
COPY NexusFlow.Shared/NexusFlow.Shared.csproj NexusFlow.Shared/
COPY Modules/Auth/NexusFlow.Auth.Abstractions/NexusFlow.Auth.Abstractions.csproj Modules/Auth/NexusFlow.Auth.Abstractions/
COPY Modules/Auth/NexusFlow.Auth/NexusFlow.Auth.csproj Modules/Auth/NexusFlow.Auth/
COPY Modules/Workflows/NexusFlow.Workflows.Abstractions/NexusFlow.Workflows.Abstractions.csproj Modules/Workflows/NexusFlow.Workflows.Abstractions/
COPY Modules/Workflows/NexusFlow.Workflows/NexusFlow.Workflows.csproj Modules/Workflows/NexusFlow.Workflows/
COPY Modules/Executions/NexusFlow.Executions.Abstractions/NexusFlow.Executions.Abstractions.csproj Modules/Executions/NexusFlow.Executions.Abstractions/
COPY Modules/Executions/NexusFlow.Executions/NexusFlow.Executions.csproj Modules/Executions/NexusFlow.Executions/
COPY Modules/Integrations/NexusFlow.Integrations.Abstractions/NexusFlow.Integrations.Abstractions.csproj Modules/Integrations/NexusFlow.Integrations.Abstractions/
COPY Modules/Integrations/NexusFlow.Integrations/NexusFlow.Integrations.csproj Modules/Integrations/NexusFlow.Integrations/
COPY tests/NexusFlow.IntegrationTests/NexusFlow.IntegrationTests.csproj tests/NexusFlow.IntegrationTests/

RUN dotnet restore NexusFlow.Api/NexusFlow.Api.csproj

# Copy source and publish
COPY . .
RUN dotnet publish NexusFlow.Api/NexusFlow.Api.csproj -c Release -o /app/publish \
    /p:UseAppHost=false /p:TreatWarningsAsErrors=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
RUN mkdir -p /app/keys && chown -R app:app /app/keys
USER app
EXPOSE 5080
ENV ASPNETCORE_URLS=http://+:5080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=true
ENTRYPOINT ["dotnet", "NexusFlow.Api.dll"]
