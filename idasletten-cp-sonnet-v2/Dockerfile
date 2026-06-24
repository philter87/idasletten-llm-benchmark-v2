FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Idasletten/Idasletten.csproj", "Idasletten/"]
RUN dotnet restore "Idasletten/Idasletten.csproj"
COPY . .
WORKDIR "/src/Idasletten"
RUN dotnet build "Idasletten.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Idasletten.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "Idasletten.dll"]
