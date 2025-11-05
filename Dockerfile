# ===== BUILD STAGE =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY StreamingZeiger.sln .

# Copy project files (alle Projekte in der Solution)
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.API/*.csproj ./StreamingZeiger.API/
COPY StreamingZeiger.AppHost/*.csproj ./StreamingZeiger.AppHost/
COPY StreamingZeiger.ServiceDefaults/*.csproj ./StreamingZeiger.ServiceDefaults/
COPY StreamingZeiger.Tests/*.csproj ./StreamingZeiger.Tests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Publish the main API project
RUN dotnet publish StreamingZeiger.API/StreamingZeiger.API.csproj -c Release -o /app/publish

# ===== RUNTIME STAGE =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StreamingZeiger.dll"]
