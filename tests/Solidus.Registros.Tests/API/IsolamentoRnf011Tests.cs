using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Solidus.Registros.API.API.Controllers;
using Solidus.Registros.API.Application.Commands;
using Solidus.Registros.API.Application.DTOs;

namespace Solidus.Registros.Tests.API;

public sealed class IsolamentoRnf011Tests
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly Guid ComercianteA = Guid.NewGuid();
    private static readonly Guid ComercianteB = Guid.NewGuid();

    private static LancamentosController CriarController(IMediator mediator, Guid comercianteId)
    {
        var controller = new LancamentosController(mediator);
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
    public async Task Post_UsaComercianteIdDoToken_NaoPermiteRegistrarEmNomeDeOutroComerciante()
    {
        var mediator = Substitute.For<IMediator>();
        var dto = new LancamentoDto(Guid.NewGuid(), ComercianteA, "CREDITO", 100m, Hoje, "chave-1", null, DateTime.UtcNow);
        mediator.Send(Arg.Any<RegistrarLancamentoCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegistrarLancamentoResult(dto, Criado: true));

        var request = new RegistrarLancamentoRequest("chave-1", "CREDITO", 100m, Hoje, null);
        await CriarController(mediator, ComercianteA).Post(request, CancellationToken.None);

        await mediator.Received(1).Send(
            Arg.Is<RegistrarLancamentoCommand>(c => c.ComercianteId == ComercianteA),
            Arg.Any<CancellationToken>());

        await mediator.DidNotReceive().Send(
            Arg.Is<RegistrarLancamentoCommand>(c => c.ComercianteId == ComercianteB),
            Arg.Any<CancellationToken>());
    }
}
