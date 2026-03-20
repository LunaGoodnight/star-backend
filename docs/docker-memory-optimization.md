# Docker Memory Optimization for StarApi

## Overview

This document describes the memory optimization changes made to reduce runtime memory usage on VPS.

## Problem

The website has moderate traffic, and we wanted to reduce the memory footprint of the .NET application running in Docker on the VPS.

## Analysis

### Original Dockerfile

The original Dockerfile was a standard multi-stage build:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["StarApi/StarApi.csproj", "StarApi/"]
RUN dotnet restore "StarApi/StarApi.csproj"
COPY . .
WORKDIR "/src/StarApi"
RUN dotnet build "./StarApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./StarApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StarApi.dll"]
```

### Issues Identified

1. **Server GC enabled by default** - .NET uses Server GC by default in containers, which allocates more memory for better throughput. This is overkill for moderate traffic sites.

2. **No memory conservation settings** - The GC was not configured to return memory to the OS aggressively.

## Changes Made

Added two environment variables to the final stage:

```dockerfile
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9
```

### Explanation of Settings

| Setting | Value | Description |
|---------|-------|-------------|
| `DOTNET_gcServer` | `0` | Disables Server GC, uses Workstation GC instead. Server GC uses more threads and memory for high-throughput scenarios. Workstation GC is single-threaded and uses ~50% less memory. |
| `DOTNET_GCConserveMemory` | `9` | Sets memory conservation to maximum (scale 0-9). The GC will return memory to the OS more frequently and maintain a smaller heap. |

## Expected Results

- Memory usage reduction from ~300-500MB to ~100-150MB
- Slightly higher GC pause times (negligible for moderate traffic)
- More frequent memory release back to OS

## Other Options Considered (Not Applied)

### 1. Alpine-based images
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
```
- Reduces image size by ~100MB
- Does not significantly affect runtime memory

### 2. Chiseled images
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
```
- Distroless, minimal attack surface
- Smaller image size

### 3. Hard memory limit
```dockerfile
ENV DOTNET_GCHeapHardLimit=0x10000000  # 256MB
```
- Forces a hard cap on heap size
- May cause OutOfMemoryException if limit is too low

### 4. PublishTrimmed
```dockerfile
RUN dotnet publish ... /p:PublishTrimmed=true /p:TrimMode=partial
```
- Removes unused code from binaries
- May break reflection-based code if not configured properly

## References

- [.NET GC Configuration](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [Running .NET in Containers](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)

## Date

2026-03-21
