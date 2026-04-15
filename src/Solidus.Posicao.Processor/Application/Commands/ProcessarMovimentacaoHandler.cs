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
    public async Task Handle(ProcessarMovimentacaoCommand cmd, CancellationToken cancellationToken)
    {
        if (await eventoRepo.ExisteAsync(cmd.EventoId, cancellationToken))
        {
            metrics.EventosDuplicadosTotal.Add(1);
            return;
        }

        var sw = Stopwatch.StartNew();

        var posicao = await posicaoRepo.ObterOuCriarAsync(cmd.ComercianteId, cmd.DataCompetencia, cancellationToken);
        posicao.AplicarMovimentacao(cmd.Tipo, cmd.Valor);

        var evento = EventoProcessado.Registrar(cmd.EventoId, "MovimentacaoRegistrada");
        await eventoRepo.AdicionarAsync(evento, cancellationToken);

        try
        {
            await unitOfWork.CommitAsync(cancellationToken);
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
