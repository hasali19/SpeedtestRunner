FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

COPY *.sln .
COPY src/SpeedtestRunner/*.csproj ./src/SpeedtestRunner/
RUN dotnet restore

COPY src/SpeedtestRunner/. ./src/SpeedtestRunner/
WORKDIR /app/src/SpeedtestRunner
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
WORKDIR /app
COPY --from=build /app/src/SpeedtestRunner/out ./
ENTRYPOINT ["dotnet", "SpeedtestRunner.dll"]
