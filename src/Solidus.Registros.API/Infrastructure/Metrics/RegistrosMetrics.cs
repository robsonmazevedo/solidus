using System.Diagnostics.Metrics;

namespace Solidus.Registros.API.Infrastructure.Metrics;

public sealed class RegistrosMetrics : IDisposable
{
    public const string MeterName = "registros-api";

    private readonly Meter _meter;
    private long _outboxPendentes;
    private double _outboxIdadeMaximaSegundos;

    public Counter<long> LancamentosTotal { get; }
    public Counter<long> LancamentosErroTotal { get; }
    public Histogram<double> HttpDuracaoSegundos { get; }
    public Counter<long> OutboxPublicadosTotal { get; }

    public RegistrosMetrics()
    {
        _meter = new Meter(MeterName);

        LancamentosTotal = _meter.CreateCounter<long>(
            "registros_lancamentos_total",
            description: "Total de lançamentos registrados com sucesso");

        LancamentosErroTotal = _meter.CreateCounter<long>(
            "registros_lancamentos_erro_total",
            description: "Total de lançamentos rejeitados por erro");

        HttpDuracaoSegundos = _meter.CreateHistogram<double>(
            "registros_http_duracao_segundos",
            unit: "s",
            description: "Latência das requisições HTTP");

        OutboxPublicadosTotal = _meter.CreateCounter<long>(
            "registros_outbox_publicados_total",
            description: "Eventos publicados pelo relay com sucesso");

        _meter.CreateObservableGauge<long>(
            "registros_outbox_pendentes",
            () => Volatile.Read(ref _outboxPendentes),
            description: "Eventos na outbox com status PENDENTE");

        _meter.CreateObservableGauge<double>(
            "registros_outbox_idade_maxima_segundos",
            () => Volatile.Read(ref _outboxIdadeMaximaSegundos),
            unit: "s",
            description: "Tempo em segundos do evento pendente mais antigo na outbox");
    }

    public void AtualizarEstadoOutbox(long pendentes, double idadeMaximaSegundos)
    {
        Volatile.Write(ref _outboxPendentes, pendentes);
        Volatile.Write(ref _outboxIdadeMaximaSegundos, idadeMaximaSegundos);
    }

    public void Dispose() => _meter.Dispose();
}
