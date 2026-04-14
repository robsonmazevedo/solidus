using System.Diagnostics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Infrastructure.Metrics;
using Solidus.Posicao.Processor.Infrastructure.Persistence;
using Solidus.Posicao.Processor.Infrastructure.Repositories;

namespace Solidus.Posicao.Processor.Application.Commands;

public sealed class ProcessarMovimentacaoHandler(
    IPosicaoDiariaRepository posicaoRepo,
    IEventoProcessadoRepository eventoRepo,
    IUnitOfWork unitOfWork,
    ProcessorMetrics metrics) : IRequestHandler<ProcessarMovimentacaoCommand>
{
    public async Task Handle(ProcessarMovimentacaoCommand cmd, CancellationToken ct)
    {
        if (await eventoRepo.ExisteAsync(cmd.EventoId, ct))
        {
            metrics.EventosDuplicadosTotal.Add(1);
            return;
        }

        var sw = Stopwatch.StartNew();

        var posicao = await posicaoRepo.ObterOuCriarAsync(cmd.ComercianteId, cmd.DataCompetencia, ct);
        posicao.AplicarMovimentacao(cmd.Tipo, cmd.Valor);

        var evento = EventoProcessado.Registrar(cmd.EventoId, "MovimentacaoRegistrada");
        await eventoRepo.AdicionarAsync(evento, ct);

        try
        {
            await unitOfWork.CommitAsync(ct);
            sw.Stop();
            metrics.EventosProcessadosTotal.Add(1);
            metrics.DuracaoProcessamentoSegundos.Record(sw.Elapsed.TotalSeconds);
        }
        catch (DbUpdateException)
        {
            // Race condition: outro worker processou o mesmo evento
        }
    }
}
