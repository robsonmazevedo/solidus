using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Solidus.Contracts.Events;
using Solidus.Registros.API.Application.Commands;
using Solidus.Registros.API.Domain.Entities;
using Solidus.Registros.API.Infrastructure.Metrics;
using Solidus.Registros.API.Infrastructure.Outbox;
using Solidus.Registros.API.Infrastructure.Persistence;
using Solidus.Registros.API.Infrastructure.Repositories;

namespace Solidus.Registros.Tests.Application;

public sealed class RegistrarLancamentoHandlerTests : IDisposable
{
    private readonly ILancamentoRepository _lancamentoRepo = Substitute.For<ILancamentoRepository>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegistrosMetrics _metrics = new();

    private RegistrarLancamentoHandler CriarHandler() =>
        new(_lancamentoRepo, _outboxRepo, _unitOfWork, _metrics);

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private static RegistrarLancamentoCommand CriarCommand(string chave = "chave-1") =>
        new(Guid.NewGuid(), chave, "CREDITO", 100m, Hoje, null);

    [Fact]
    public async Task Handle_NovoLancamento_RetornaCriadoTrue()
    {
        _lancamentoRepo.BuscarPorChaveIdempotenciaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Lancamento?)null);

        var result = await CriarHandler().Handle(CriarCommand(), CancellationToken.None);

        result.Criado.Should().BeTrue();
        result.Lancamento.Should().NotBeNull();
        await _lancamentoRepo.Received(1).AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
        await _outboxRepo.Received(1).AdicionarAsync(Arg.Any<OutboxEntry>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChaveIdempotenciaDuplicada_RetornaCriadoFalse()
    {
        var existente = Lancamento.Registrar(Guid.NewGuid(), "CREDITO", 100m, Hoje, "chave-1");
        _lancamentoRepo.BuscarPorChaveIdempotenciaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existente);

        var result = await CriarHandler().Handle(CriarCommand("chave-1"), CancellationToken.None);

        result.Criado.Should().BeFalse();
        result.Lancamento.Id.Should().Be(existente.Id);
        await _lancamentoRepo.DidNotReceive().AdicionarAsync(Arg.Any<Lancamento>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RaceConditionDbUpdateException_RetornaCriadoFalse()
    {
        var existente = Lancamento.Registrar(Guid.NewGuid(), "CREDITO", 100m, Hoje, "chave-race");
        _lancamentoRepo.BuscarPorChaveIdempotenciaAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Lancamento?)null, existente);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new DbUpdateException()));

        var result = await CriarHandler().Handle(CriarCommand("chave-race"), CancellationToken.None);

        result.Criado.Should().BeFalse();
        result.Lancamento.Id.Should().Be(existente.Id);
    }

    public void Dispose() => _metrics.Dispose();
}
