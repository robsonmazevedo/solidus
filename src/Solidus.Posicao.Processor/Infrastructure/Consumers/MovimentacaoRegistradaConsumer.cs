using MassTransit;
using MediatR;
using Solidus.Posicao.Processor.Application.Commands;
using Solidus.Contracts.Events;

namespace Solidus.Posicao.Processor.Infrastructure.Consumers;

public sealed class MovimentacaoRegistradaConsumer(IMediator mediator) : IConsumer<MovimentacaoRegistradaEvent>
{
    public Task Consume(ConsumeContext<MovimentacaoRegistradaEvent> context)
    {
        var evt = context.Message;
        var cmd = new ProcessarMovimentacaoCommand(
            evt.EventoId,
            evt.LancamentoId,
            evt.ComercianteId,
            evt.Tipo,
            evt.Valor,
            evt.DataCompetencia);

        return mediator.Send(cmd, context.CancellationToken);
    }
}
