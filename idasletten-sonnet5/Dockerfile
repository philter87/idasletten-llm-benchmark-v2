FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Idasletten/Idasletten.csproj Idasletten/
RUN dotnet restore Idasletten/Idasletten.csproj

COPY Idasletten/ Idasletten/
RUN dotnet publish Idasletten/Idasletten.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Idasletten.dll"]
