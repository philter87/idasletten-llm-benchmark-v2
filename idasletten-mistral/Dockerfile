# Idasletten Dockerfile for Fly.io deployment

# Use official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Idasletten.sln", "."]
COPY ["Idasletten/Idasletten.csproj", "Idasletten/"]
COPY ["Idasletten.Tests/Idasletten.Tests.csproj", "Idasletten.Tests/"]

# Restore dependencies
RUN dotnet restore "Idasletten/Idasletten.csproj"

# Copy remaining source code
COPY ["Idasletten/.", "Idasletten/."]
COPY ["Idasletten.Tests/.", "Idasletten.Tests/."]

# Build the application
RUN dotnet build "Idasletten/Idasletten.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "Idasletten/Idasletten.csproj" -c Release -o /app/publish \
    --no-restore \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=true

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Create data directory
RUN mkdir -p /data

# Set connection string to use SQLite in the data directory
ENV ConnectionStrings__DefaultConnection="Data Source=/data/idasletten.db"
ENV ASPNETCORE_ENVIRONMENT="Production"
ENV ASPNETCORE_URLS="http://+:8080"

# Expose port 8080
EXPOSE 8080

# Ensure data directory exists and has correct permissions
RUN chmod -R 777 /data

# Entry point
ENTRYPOINT ["dotnet", "Idasletten.dll"]
