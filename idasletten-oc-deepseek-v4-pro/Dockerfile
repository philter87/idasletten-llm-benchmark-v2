FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Idasletten/Idasletten.csproj", "Idasletten/"]
RUN dotnet restore "Idasletten/Idasletten.csproj"
COPY . .
WORKDIR "/src/Idasletten"
RUN dotnet build "Idasletten.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Idasletten.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Idasletten.dll"]
