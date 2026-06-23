FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Idasletten/Idasletten.csproj Idasletten/
COPY Idasletten.Tests/Idasletten.Tests.csproj Idasletten.Tests/
RUN dotnet restore Idasletten/Idasletten.csproj

COPY . .
WORKDIR /src/Idasletten
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Idasletten.dll"]
