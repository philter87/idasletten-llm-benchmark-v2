FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Idasletten/Idasletten.csproj", "Idasletten/"]
RUN dotnet restore "Idasletten/Idasletten.csproj"
COPY . .
WORKDIR "/src/Idasletten"
RUN dotnet build "Idasletten.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Idasletten.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /data
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Idasletten.dll"]
