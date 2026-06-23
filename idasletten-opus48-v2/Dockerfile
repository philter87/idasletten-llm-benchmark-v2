# Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Idasletten.slnx ./
COPY Idasletten/Idasletten.csproj Idasletten/
COPY Idasletten.Tests/Idasletten.Tests.csproj Idasletten.Tests/
RUN dotnet restore Idasletten/Idasletten.csproj
COPY . .
RUN dotnet publish Idasletten/Idasletten.csproj -c Release -o /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# SQLite database lives on the mounted Fly volume.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__Default="Data Source=/data/idasletten.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "Idasletten.dll"]
