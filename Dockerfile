# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StreamingZeiger.sln ./
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/

# Restore Main project dependencies
RUN dotnet restore StreamingZeiger/StreamingZeiger.csproj

# Copy all source files
COPY StreamingZeiger/. ./StreamingZeiger/
COPY StreamingZeiger.API/. ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/. ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/. ./StreamingZeiger.ServiceDefaults/

# Remove library appsettings to avoid conflicts
RUN rm -f StreamingZeiger/appsettings*.json

# Build API project (optional)
RUN dotnet build StreamingZeiger.API/StreamingZeiger.API.csproj -c Release

# Publish Main project
RUN dotnet publish StreamingZeiger/StreamingZeiger.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Ensure /app is writable (SQLite will create DB here)
RUN chown -R app:app /app

EXPOSE 80

ENTRYPOINT ["dotnet", "StreamingZeiger.dll"]
