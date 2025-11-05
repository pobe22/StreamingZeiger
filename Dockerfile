# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only solution and API project csproj files first (for restore caching)
COPY StreamingZeiger.sln .
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/
COPY StreamingZeiger.Tests/*.csproj ./StreamingZeiger.Tests/

# Restore dependencies
RUN dotnet restore StreamingZeiger.API/StreamingZeiger.API.csproj

# Copy only API project files (including appsettings) and necessary shared code
COPY StreamingZeiger.API/. ./StreamingZeiger.API/
COPY StreamingZeiger/. ./StreamingZeiger/

# Publish only the API project
RUN dotnet publish StreamingZeiger.API/StreamingZeiger.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StreamingZeiger.API.dll"]
