FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Registros.API/Solidus.Registros.API.csproj", "src/Solidus.Registros.API/"]
RUN dotnet restore "src/Solidus.Registros.API/Solidus.Registros.API.csproj"

COPY . .
WORKDIR "/src/src/Solidus.Registros.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Solidus.Registros.API.dll"]
