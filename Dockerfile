# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY StreamingZeiger/*.csproj ./StreamingZeiger/
COPY StreamingZeiger.Tests/*.csproj ./StreamingZeiger.Tests/
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish StreamingZeiger/StreamingZeiger.csproj -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "StreamingZeiger.dll"]
