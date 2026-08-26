# syntax=docker/dockerfile:1
# Multi-stage build for PetroVisorLite.Api (.NET LTS)
# NOTE: This Dockerfile is written speculatively against the documented backend
# convention (src/PetroVisorLite.Api/PetroVisorLite.Api.csproj) before the
# solution existed. Verify paths once Leia's scaffold lands.

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0

# ---- Build stage ----
FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

# Copy csproj(s) first for better layer caching, then restore
COPY src/PetroVisorLite.Api/PetroVisorLite.Api.csproj src/PetroVisorLite.Api/

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish src/PetroVisorLite.Api/PetroVisorLite.Api.csproj \
    -c Release \
    -o /app/publish

# ---- Runtime stage ----
FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
WORKDIR /app

# Run as non-root (the aspnet base image ships a built-in "app" user, uid 64198)
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app

ENTRYPOINT ["dotnet", "PetroVisorLite.Api.dll"]
