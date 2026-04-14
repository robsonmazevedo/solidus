using System.Diagnostics.Metrics;

namespace Solidus.Posicao.API.Infrastructure.Metrics;

public sealed class PosicaoMetrics : IDisposable
{
    public const string MeterName = "posicao-api";

    private readonly Meter _meter;

    public Counter<long> ConsultasTotal { get; }
    public Histogram<double> HttpDuracaoSegundos { get; }
    public Counter<long> CacheHitTotal { get; }
    public Counter<long> CacheMissTotal { get; }

    public PosicaoMetrics()
    {
        _meter = new Meter(MeterName);

        ConsultasTotal = _meter.CreateCounter<long>(
            "posicao_consultas_total",
            description: "Total de consultas de posição diária");

        HttpDuracaoSegundos = _meter.CreateHistogram<double>(
            "posicao_http_duracao_segundos",
            unit: "s",
            description: "Latência das requisições HTTP");

        CacheHitTotal = _meter.CreateCounter<long>(
            "posicao_cache_hit_total",
            description: "Consultas respondidas pelo Redis");

        CacheMissTotal = _meter.CreateCounter<long>(
            "posicao_cache_miss_total",
            description: "Consultas que caíram no banco por ausência de cache");
    }

    public void Dispose() => _meter.Dispose();
}
