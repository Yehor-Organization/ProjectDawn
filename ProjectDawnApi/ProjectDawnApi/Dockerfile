# =========================
# Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# =========================
# Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj first (restore caching)
COPY ProjectDawnApi.csproj ./
RUN dotnet restore "ProjectDawnApi.csproj"

# Copy everything else
COPY . .
RUN dotnet build "ProjectDawnApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# =========================
# Publish
# =========================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ProjectDawnApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# =========================
# Final
# =========================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectDawnApi.dll"]
