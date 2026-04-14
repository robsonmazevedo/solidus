FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj", "src/Solidus.Posicao.Processor/"]
RUN dotnet restore "src/Solidus.Posicao.Processor/Solidus.Posicao.Processor.csproj"

COPY . .
WORKDIR "/src/src/Solidus.Posicao.Processor"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8082

ENV ASPNETCORE_URLS=http://+:8082

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Solidus.Posicao.Processor.dll"]
