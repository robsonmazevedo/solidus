using FluentAssertions;
using MassTransit;
using MediatR;
using NSubstitute;
using Solidus.Contracts.Events;
using Solidus.Posicao.Processor.Application.Commands;
using Solidus.Posicao.Processor.Infrastructure.Consumers;

namespace Solidus.Posicao.Processor.Tests.Infrastructure;

public sealed class MovimentacaoRegistradaConsumerTests
{
    [Fact]
    public async Task Consume_CriaCommandCorretoEEnviaParaMediator()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ProcessarMovimentacaoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Unit.Value));

        var consumer = new MovimentacaoRegistradaConsumer(mediator);

        var evt = new MovimentacaoRegistradaEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CREDITO", 200m, DateOnly.FromDateTime(DateTime.Today));

        var context = Substitute.For<ConsumeContext<MovimentacaoRegistradaEvent>>();
        context.Message.Returns(evt);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        await mediator.Received(1).Send(
            Arg.Is<ProcessarMovimentacaoCommand>(c =>
                c.EventoId == evt.EventoId &&
                c.LancamentoId == evt.LancamentoId &&
                c.ComercianteId == evt.ComercianteId &&
                c.Tipo == evt.Tipo &&
                c.Valor == evt.Valor &&
                c.DataCompetencia == evt.DataCompetencia),
            Arg.Any<CancellationToken>());
    }
}
