# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StreamingZeiger.sln ./
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/
COPY StreamingZeiger.Tests/*.csproj ./StreamingZeiger.Tests/

# Restore dependencies
RUN dotnet restore StreamingZeiger/StreamingZeiger.csproj

# Copy all source code
COPY StreamingZeiger/. ./StreamingZeiger/
COPY StreamingZeiger.API/. ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/. ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/. ./StreamingZeiger.ServiceDefaults/
COPY StreamingZeiger.Tests/. ./StreamingZeiger.Tests/

# Remove library appsettings to avoid conflicts
RUN rm -f StreamingZeiger/appsettings*.json

# Build API project (optional)
RUN dotnet build StreamingZeiger/StreamingZeiger.API.csproj -c Release

# Publish main project
RUN dotnet publish StreamingZeiger/StreamingZeiger.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user
RUN useradd -ms /bin/bash appuser
COPY --from=build /app/publish .

# Give write permission to SQLite
RUN chown -R appuser:appuser /app
USER appuser

EXPOSE 80

ENTRYPOINT ["dotnet", "StreamingZeiger.dll"]
