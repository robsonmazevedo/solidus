FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Posicao.API/Solidus.Posicao.API.csproj", "src/Solidus.Posicao.API/"]
RUN dotnet restore "src/Solidus.Posicao.API/Solidus.Posicao.API.csproj"

COPY ["src/Solidus.Posicao.API/API/", "src/Solidus.Posicao.API/API/"]
COPY ["src/Solidus.Posicao.API/Application/", "src/Solidus.Posicao.API/Application/"]
COPY ["src/Solidus.Posicao.API/Domain/", "src/Solidus.Posicao.API/Domain/"]
COPY ["src/Solidus.Posicao.API/Infrastructure/", "src/Solidus.Posicao.API/Infrastructure/"]
COPY ["src/Solidus.Posicao.API/Program.cs", "src/Solidus.Posicao.API/"]
COPY ["src/Solidus.Posicao.API/appsettings.json", "src/Solidus.Posicao.API/"]
COPY ["src/Solidus.Posicao.API/appsettings.Development.json", "src/Solidus.Posicao.API/"]
WORKDIR "/src/src/Solidus.Posicao.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Solidus.Posicao.API.dll"]
