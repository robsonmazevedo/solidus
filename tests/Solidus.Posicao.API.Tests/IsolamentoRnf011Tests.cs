using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Solidus.Posicao.API.API.Controllers;
using Solidus.Posicao.API.Application.DTOs;
using Solidus.Posicao.API.Application.Handlers;
using Solidus.Posicao.API.Application.Queries;
using Solidus.Posicao.API.Infrastructure.Cache;
using Solidus.Posicao.API.Infrastructure.Metrics;
using Solidus.Posicao.API.Infrastructure.Repositories;

namespace Solidus.Posicao.API.Tests;

public sealed class IsolamentoRnf011Tests : IDisposable
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly Guid ComercianteA = Guid.NewGuid();
    private static readonly Guid ComercianteB = Guid.NewGuid();

    private readonly PosicaoMetrics _metrics = new();

    public void Dispose() => _metrics.Dispose();

    private static PosicaoController CriarController(IMediator mediator, Guid comercianteId)
    {
        var controller = new PosicaoController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim("comerciante_id", comercianteId.ToString()) }))
            }
        };
        return controller;
    }

    [Fact]
    public async Task ConsultarDiaria_ComercianteIdVemDoToken_NaoDeParametroExterno()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ConsultarPosicaoDiariaQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PosicaoDiariaDto(Hoje, 0, 0, 0, DateTime.UtcNow));

        await CriarController(mediator, ComercianteA).ConsultarDiaria(Hoje, CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<ConsultarPosicaoDiariaQuery>(q => q.ComercianteId == ComercianteA),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_ConsultaRepositorioApenasComComercianteIdDoToken()
    {
        var cache = Substitute.For<IPosicaoCacheService>();
        var repository = Substitute.For<IPosicaoDiariaReadRepository>();

        cache.ObterAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((PosicaoDiariaDto?)null);
        repository.ObterAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Domain.ReadModels.PosicaoDiaria?)null);

        var handler = new ConsultarPosicaoDiariaHandler(cache, repository, _metrics);
        await handler.Handle(new ConsultarPosicaoDiariaQuery(ComercianteA, Hoje), CancellationToken.None);

        await repository.Received(1).ObterAsync(ComercianteA, Hoje, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().ObterAsync(ComercianteB, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_CacheEscopadoPorComercianteId_NaoUsaCacheDeOutroComerciante()
    {
        var cache = Substitute.For<IPosicaoCacheService>();
        var repository = Substitute.For<IPosicaoDiariaReadRepository>();

        cache.ObterAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((PosicaoDiariaDto?)null);
        repository.ObterAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns((Domain.ReadModels.PosicaoDiaria?)null);

        var handler = new ConsultarPosicaoDiariaHandler(cache, repository, _metrics);
        await handler.Handle(new ConsultarPosicaoDiariaQuery(ComercianteA, Hoje), CancellationToken.None);

        await cache.Received(1).ObterAsync(ComercianteA, Hoje, Arg.Any<CancellationToken>());
        await cache.DidNotReceive().ObterAsync(ComercianteB, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }
}
