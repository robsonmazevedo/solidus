FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj", "src/Solidus.Posicao.Processor/"]
RUN dotnet restore "src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj"

COPY ["src/Solidus.Posicao.Processor/Application/", "src/Solidus.Posicao.Processor/Application/"]
COPY ["src/Solidus.Posicao.Processor/Domain/", "src/Solidus.Posicao.Processor/Domain/"]
COPY ["src/Solidus.Posicao.Processor/Infrastructure/", "src/Solidus.Posicao.Processor/Infrastructure/"]
COPY ["src/Solidus.Posicao.Processor/Migrations/", "src/Solidus.Posicao.Processor/Migrations/"]
COPY ["src/Solidus.Posicao.Processor/Program.cs", "src/Solidus.Posicao.Processor/"]
COPY ["src/Solidus.Posicao.Processor/Worker.cs", "src/Solidus.Posicao.Processor/"]
COPY ["src/Solidus.Posicao.Processor/appsettings.json", "src/Solidus.Posicao.Processor/"]
COPY ["src/Solidus.Posicao.Processor/appsettings.Development.json", "src/Solidus.Posicao.Processor/"]
WORKDIR "/src/src/Solidus.Posicao.Processor"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Solidus.Posicao.Processor.dll"]
