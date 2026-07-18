FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Idasletten/Idasletten.csproj", "Idasletten/"]
RUN dotnet restore "Idasletten/Idasletten.csproj"
COPY . .
RUN dotnet publish "Idasletten/Idasletten.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Idasletten.dll"]
