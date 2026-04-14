using FluentAssertions;
using NSubstitute;
using Solidus.Posicao.API.Application.DTOs;
using Solidus.Posicao.API.Application.Handlers;
using Solidus.Posicao.API.Application.Queries;
using Solidus.Posicao.API.Domain.ReadModels;
using Solidus.Posicao.API.Infrastructure.Cache;
using Solidus.Posicao.API.Infrastructure.Metrics;
using Solidus.Posicao.API.Infrastructure.Repositories;

namespace Solidus.Posicao.API.Tests.Application;

public sealed class ConsultarPosicaoDiariaHandlerTests : IDisposable
{
    private readonly IPosicaoCacheService _cache = Substitute.For<IPosicaoCacheService>();
    private readonly IPosicaoDiariaReadRepository _repository = Substitute.For<IPosicaoDiariaReadRepository>();
    private readonly PosicaoMetrics _metrics = new();

    private ConsultarPosicaoDiariaHandler CriarHandler() =>
        new(_cache, _repository, _metrics);

    private static readonly Guid ComercianteId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Handle_CacheHit_RetornaDtoSemConsultarRepositorio()
    {
        var cached = new PosicaoDiariaDto(Hoje, 200m, 50m, 150m, DateTime.UtcNow);
        _cache.ObterAsync(ComercianteId, Hoje, Arg.Any<CancellationToken>()).Returns(cached);

        var result = await CriarHandler().Handle(new ConsultarPosicaoDiariaQuery(ComercianteId, Hoje), CancellationToken.None);

        result.Should().Be(cached);
        await _repository.DidNotReceive().ObterAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMissComPosicao_RetornaDtoEGravaCache()
    {
        _cache.ObterAsync(ComercianteId, Hoje, Arg.Any<CancellationToken>()).Returns((PosicaoDiariaDto?)null);
        var posicao = CriarPosicaoDiaria(ComercianteId, Hoje, 300m, 100m);
        _repository.ObterAsync(ComercianteId, Hoje, Arg.Any<CancellationToken>()).Returns(posicao);

        var result = await CriarHandler().Handle(new ConsultarPosicaoDiariaQuery(ComercianteId, Hoje), CancellationToken.None);

        result.TotalCreditos.Should().Be(300m);
        result.TotalDebitos.Should().Be(100m);
        result.Saldo.Should().Be(200m);
        await _cache.Received(1).GravarAsync(ComercianteId, Hoje, Arg.Any<PosicaoDiariaDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMissSemPosicao_RetornaZeros()
    {
        _cache.ObterAsync(ComercianteId, Hoje, Arg.Any<CancellationToken>()).Returns((PosicaoDiariaDto?)null);
        _repository.ObterAsync(ComercianteId, Hoje, Arg.Any<CancellationToken>()).Returns((PosicaoDiaria?)null);

        var result = await CriarHandler().Handle(new ConsultarPosicaoDiariaQuery(ComercianteId, Hoje), CancellationToken.None);

        result.TotalCreditos.Should().Be(0);
        result.TotalDebitos.Should().Be(0);
        result.Saldo.Should().Be(0);
        await _cache.DidNotReceive().GravarAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<PosicaoDiariaDto>(), Arg.Any<CancellationToken>());
    }

    private static PosicaoDiaria CriarPosicaoDiaria(Guid comercianteId, DateOnly data, decimal creditos, decimal debitos)
    {
        var posicao = new PosicaoDiaria();
        var type = typeof(PosicaoDiaria);
        type.GetProperty(nameof(PosicaoDiaria.Id))!.SetValue(posicao, Guid.NewGuid());
        type.GetProperty(nameof(PosicaoDiaria.ComercianteId))!.SetValue(posicao, comercianteId);
        type.GetProperty(nameof(PosicaoDiaria.DataPosicao))!.SetValue(posicao, data);
        type.GetProperty(nameof(PosicaoDiaria.TotalCreditos))!.SetValue(posicao, creditos);
        type.GetProperty(nameof(PosicaoDiaria.TotalDebitos))!.SetValue(posicao, debitos);
        type.GetProperty(nameof(PosicaoDiaria.Saldo))!.SetValue(posicao, creditos - debitos);
        type.GetProperty(nameof(PosicaoDiaria.AtualizadoEm))!.SetValue(posicao, DateTime.UtcNow);
        return posicao;
    }

    public void Dispose() => _metrics.Dispose();
}
