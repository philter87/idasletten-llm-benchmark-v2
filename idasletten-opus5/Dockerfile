# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Idasletten/Idasletten.csproj Idasletten/
COPY Idasletten.Tests/Idasletten.Tests.csproj Idasletten.Tests/
RUN dotnet restore Idasletten/Idasletten.csproj

COPY . .
RUN dotnet publish Idasletten/Idasletten.csproj -c Release -o /app/publish /p:UseAppHost=false

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Fly mounts a volume here - the SQLite file lives on it, see fly.toml.
RUN mkdir -p /data
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Idasletten="Data Source=/data/idasletten.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "Idasletten.dll"]
