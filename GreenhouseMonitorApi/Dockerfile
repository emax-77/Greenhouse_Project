FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GreenhouseMonitorApi/GreenhouseMonitorApi.csproj", "GreenhouseMonitorApi/"]
RUN dotnet restore "GreenhouseMonitorApi/GreenhouseMonitorApi.csproj"
COPY . .
WORKDIR "/src/GreenhouseMonitorApi"
RUN dotnet publish "GreenhouseMonitorApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GreenhouseMonitorApi.dll"]
