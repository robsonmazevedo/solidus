using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Solidus.Posicao.API.API.Controllers;
using Solidus.Posicao.API.Application.DTOs;
using Solidus.Posicao.API.Application.Queries;

namespace Solidus.Posicao.API.Tests.API;

public sealed class PosicaoControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private static readonly Guid ComercianteId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private PosicaoController CriarController()
    {
        var controller = new PosicaoController(_mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("comerciante_id", ComercianteId.ToString())
                }))
            }
        };
        return controller;
    }

    [Fact]
    public async Task ConsultarDiaria_DataFutura_RetornaUnprocessableEntity()
    {
        var amanha = Hoje.AddDays(1);
        var result = await CriarController().ConsultarDiaria(amanha, CancellationToken.None);
        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task ConsultarDiaria_DataValida_RetornaOk()
    {
        var dto = new PosicaoDiariaDto(Hoje, 500m, 200m, 300m, DateTime.UtcNow);
        _mediator.Send(Arg.Any<ConsultarPosicaoDiariaQuery>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await CriarController().ConsultarDiaria(Hoje, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(dto);
    }
}
