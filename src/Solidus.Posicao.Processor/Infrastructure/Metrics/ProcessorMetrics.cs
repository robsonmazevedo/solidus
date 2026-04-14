using System.Diagnostics.Metrics;

namespace Solidus.Posicao.Processor.Infrastructure.Metrics;

public sealed class ProcessorMetrics : IDisposable
{
    public const string MeterName = "posicao-processor";

    private readonly Meter _meter;

    public Counter<long> EventosProcessadosTotal { get; }
    public Counter<long> EventosDuplicadosTotal { get; }
    public Histogram<double> DuracaoProcessamentoSegundos { get; }

    public ProcessorMetrics()
    {
        _meter = new Meter(MeterName);

        EventosProcessadosTotal = _meter.CreateCounter<long>(
            "processor_eventos_processados_total",
            description: "Eventos consumidos e processados com sucesso");

        EventosDuplicadosTotal = _meter.CreateCounter<long>(
            "processor_eventos_duplicados_total",
            description: "Eventos ignorados por já terem sido processados");

        DuracaoProcessamentoSegundos = _meter.CreateHistogram<double>(
            "processor_duracao_processamento_segundos",
            unit: "s",
            description: "Tempo de processamento por evento");
    }

    public void Dispose() => _meter.Dispose();
}
