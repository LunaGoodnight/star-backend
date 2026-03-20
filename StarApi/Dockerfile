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

# Reduce runtime memory on VPS
# - gcServer=0: Use Workstation GC instead of Server GC (less memory for moderate traffic)
# - GCConserveMemory=9: Most aggressive memory conservation (scale 0-9)
ENV DOTNET_gcServer=0
ENV DOTNET_GCConserveMemory=9

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StarApi.dll"]
