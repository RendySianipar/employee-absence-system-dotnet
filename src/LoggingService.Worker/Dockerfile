# Build context: repository root  (docker build -f src/LoggingService.Worker/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/EmployeeAbsence.Shared/EmployeeAbsence.Shared.csproj src/EmployeeAbsence.Shared/
COPY src/LoggingService.Worker/LoggingService.Worker.csproj src/LoggingService.Worker/
RUN dotnet restore src/LoggingService.Worker/LoggingService.Worker.csproj
COPY src/EmployeeAbsence.Shared/ src/EmployeeAbsence.Shared/
COPY src/LoggingService.Worker/ src/LoggingService.Worker/
RUN dotnet publish src/LoggingService.Worker/LoggingService.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "LoggingService.Worker.dll"]
