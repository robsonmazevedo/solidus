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

public sealed class LancamentosControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private static readonly Guid ComercianteId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private LancamentosController CriarController(string? claimValor = null)
    {
        var controller = new LancamentosController(_mediator);
        var claims = claimValor is not null
            ? new[] { new Claim("comerciante_id", claimValor) }
            : Array.Empty<Claim>();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims))
            }
        };
        return controller;
    }

    private static RegistrarLancamentoRequest CriarRequest() =>
        new("chave-1", "CREDITO", 100m, Hoje, null);

    [Fact]
    public async Task Post_SemClaimComerciante_RetornaUnauthorized()
    {
        var result = await CriarController().Post(CriarRequest(), CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Post_ClaimComercianteInvalido_RetornaUnauthorized()
    {
        var result = await CriarController("nao-e-um-guid").Post(CriarRequest(), CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Post_LancamentoCriado_Retorna201()
    {
        var dto = new LancamentoDto(Guid.NewGuid(), ComercianteId, "CREDITO", 100m, Hoje, "chave-1", null, DateTime.UtcNow);
        _mediator.Send(Arg.Any<RegistrarLancamentoCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegistrarLancamentoResult(dto, Criado: true));

        var result = await CriarController(ComercianteId.ToString()).Post(CriarRequest(), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Post_LancamentoDuplicado_Retorna200()
    {
        var dto = new LancamentoDto(Guid.NewGuid(), ComercianteId, "CREDITO", 100m, Hoje, "chave-1", null, DateTime.UtcNow);
        _mediator.Send(Arg.Any<RegistrarLancamentoCommand>(), Arg.Any<CancellationToken>())
            .Returns(new RegistrarLancamentoResult(dto, Criado: false));

        var result = await CriarController(ComercianteId.ToString()).Post(CriarRequest(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
