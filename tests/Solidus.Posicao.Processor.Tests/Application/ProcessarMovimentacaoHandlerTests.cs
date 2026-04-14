using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Solidus.Posicao.Processor.Application.Commands;
using Solidus.Posicao.Processor.Domain.Entities;
using Solidus.Posicao.Processor.Infrastructure.Metrics;
using Solidus.Posicao.Processor.Infrastructure.Persistence;
using Solidus.Posicao.Processor.Infrastructure.Repositories;

namespace Solidus.Posicao.Processor.Tests.Application;

public sealed class ProcessarMovimentacaoHandlerTests : IDisposable
{
    private readonly IPosicaoDiariaRepository _posicaoRepo = Substitute.For<IPosicaoDiariaRepository>();
    private readonly IEventoProcessadoRepository _eventoRepo = Substitute.For<IEventoProcessadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProcessorMetrics _metrics = new();

    private ProcessarMovimentacaoHandler CriarHandler() =>
        new(_posicaoRepo, _eventoRepo, _unitOfWork, _metrics);

    [Fact]
    public async Task Handle_EventoDuplicado_IncrementaContadorDuplicadosENaoCommita()
    {
        _eventoRepo.ExisteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var cmd = new ProcessarMovimentacaoCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CREDITO", 100m,
            DateOnly.FromDateTime(DateTime.Today));

        await CriarHandler().Handle(cmd, CancellationToken.None);

        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventoNovo_Credito_ProcessaECommita()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));
        _eventoRepo.ExisteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _posicaoRepo.ObterOuCriarAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(posicao);
        _eventoRepo.AdicionarAsync(Arg.Any<EventoProcessado>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var cmd = new ProcessarMovimentacaoCommand(
            Guid.NewGuid(), Guid.NewGuid(), posicao.ComercianteId, "CREDITO", 150m, posicao.DataPosicao);

        await CriarHandler().Handle(cmd, CancellationToken.None);

        posicao.TotalCreditos.Should().Be(150m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventoNovo_Debito_ProcessaECommita()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));
        _eventoRepo.ExisteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _posicaoRepo.ObterOuCriarAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(posicao);
        _eventoRepo.AdicionarAsync(Arg.Any<EventoProcessado>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var cmd = new ProcessarMovimentacaoCommand(
            Guid.NewGuid(), Guid.NewGuid(), posicao.ComercianteId, "DEBITO", 75m, posicao.DataPosicao);

        await CriarHandler().Handle(cmd, CancellationToken.None);

        posicao.TotalDebitos.Should().Be(75m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RaceCondition_DbUpdateException_NaoPropagaExcecao()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));
        _eventoRepo.ExisteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _posicaoRepo.ObterOuCriarAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(posicao);
        _eventoRepo.AdicionarAsync(Arg.Any<EventoProcessado>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new DbUpdateException()));
        var cmd = new ProcessarMovimentacaoCommand(
            Guid.NewGuid(), Guid.NewGuid(), posicao.ComercianteId, "CREDITO", 100m, posicao.DataPosicao);

        var act = () => CriarHandler().Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    public void Dispose() => _metrics.Dispose();
}
