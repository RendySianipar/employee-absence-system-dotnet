# Build context: repository root  (docker build -f src/EmployeeService.Api/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/EmployeeAbsence.Shared/EmployeeAbsence.Shared.csproj src/EmployeeAbsence.Shared/
COPY src/EmployeeService.Api/EmployeeService.Api.csproj src/EmployeeService.Api/
RUN dotnet restore src/EmployeeService.Api/EmployeeService.Api.csproj
COPY src/EmployeeAbsence.Shared/ src/EmployeeAbsence.Shared/
COPY src/EmployeeService.Api/ src/EmployeeService.Api/
RUN dotnet publish src/EmployeeService.Api/EmployeeService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads && chown -R $APP_UID /app/wwwroot
ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000
USER $APP_UID
ENTRYPOINT ["dotnet", "EmployeeService.Api.dll"]
