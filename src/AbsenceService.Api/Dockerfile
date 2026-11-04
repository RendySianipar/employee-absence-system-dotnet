# Build context: repository root  (docker build -f src/AbsenceService.Api/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/EmployeeAbsence.Shared/EmployeeAbsence.Shared.csproj src/EmployeeAbsence.Shared/
COPY src/AbsenceService.Api/AbsenceService.Api.csproj src/AbsenceService.Api/
RUN dotnet restore src/AbsenceService.Api/AbsenceService.Api.csproj
COPY src/EmployeeAbsence.Shared/ src/EmployeeAbsence.Shared/
COPY src/AbsenceService.Api/ src/AbsenceService.Api/
RUN dotnet publish src/AbsenceService.Api/AbsenceService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:3001
EXPOSE 3001
USER $APP_UID
ENTRYPOINT ["dotnet", "AbsenceService.Api.dll"]
