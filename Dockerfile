# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files (for restore caching)
COPY StreamingZeiger.sln ./

# Copy all csproj files
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/

# Restore only API project dependencies
RUN dotnet restore StreamingZeiger.API/StreamingZeiger.API.csproj

# Copy all source files for libraries
COPY StreamingZeiger/. ./StreamingZeiger/
COPY StreamingZeiger.AppHost/. ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/. ./StreamingZeiger.ServiceDefaults/

# Copy API source files (including appsettings, Views, wwwroot)
COPY StreamingZeiger.API/. ./StreamingZeiger.API/

# Publish only the API project
RUN dotnet publish StreamingZeiger.API/StreamingZeiger.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "StreamingZeiger.API.dll"]
