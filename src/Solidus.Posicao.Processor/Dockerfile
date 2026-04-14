FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj", "src/Solidus.Posicao.Processor/"]
RUN dotnet restore "src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj"

COPY . .
WORKDIR "/src/src/Solidus.Posicao.Processor"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Solidus.Posicao.Processor.dll"]
