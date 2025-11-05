# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files (for restore caching)
COPY StreamingZeiger.sln .
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/

# Restore dependencies only for API project
RUN dotnet restore StreamingZeiger.API/StreamingZeiger.API.csproj

# Copy only API source files and its appsettings
COPY StreamingZeiger.API/. ./StreamingZeiger.API/

# Copy only shared source files (exclude appsettings)
COPY StreamingZeiger/*.cs ./StreamingZeiger/

# Publish API project
RUN dotnet publish StreamingZeiger.API/StreamingZeiger.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StreamingZeiger.API.dll"]
