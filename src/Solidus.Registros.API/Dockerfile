FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Registros.API/Solidus.Registros.API.csproj", "src/Solidus.Registros.API/"]
RUN dotnet restore "src/Solidus.Registros.API/Solidus.Registros.API.csproj"

COPY ["src/Solidus.Registros.API/API/", "src/Solidus.Registros.API/API/"]
COPY ["src/Solidus.Registros.API/Application/", "src/Solidus.Registros.API/Application/"]
COPY ["src/Solidus.Registros.API/Domain/", "src/Solidus.Registros.API/Domain/"]
COPY ["src/Solidus.Registros.API/Infrastructure/", "src/Solidus.Registros.API/Infrastructure/"]
COPY ["src/Solidus.Registros.API/Migrations/", "src/Solidus.Registros.API/Migrations/"]
COPY ["src/Solidus.Registros.API/Program.cs", "src/Solidus.Registros.API/"]
COPY ["src/Solidus.Registros.API/appsettings.json", "src/Solidus.Registros.API/"]
COPY ["src/Solidus.Registros.API/appsettings.Development.json", "src/Solidus.Registros.API/"]
WORKDIR "/src/src/Solidus.Registros.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Solidus.Registros.API.dll"]
