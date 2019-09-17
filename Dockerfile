FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

COPY *.sln .
COPY src/SpeedtestRunner/*.csproj ./src/SpeedtestRunner/
COPY ext/SpeedTest.Net/SpeedTest/SpeedTest.csproj ./ext/SpeedTest.Net/SpeedTest/
RUN dotnet restore

COPY src/SpeedtestRunner/. ./src/SpeedtestRunner/
COPY ext/SpeedTest.Net/SpeedTest/. ./ext/SpeedTest.Net/SpeedTest/
WORKDIR /app/src/SpeedtestRunner
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
WORKDIR /app
COPY --from=build /app/src/SpeedtestRunner/out ./
ENTRYPOINT ["dotnet", "SpeedtestRunner.dll"]
