using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Solidus.Registros.API.Application.Commands;
using Solidus.Registros.API.Application.DTOs;

namespace Solidus.Registros.API.API.Controllers;

[ApiController]
[Route("lancamentos")]
[Authorize]
[EnableRateLimiting("por-comerciante")]
public sealed class LancamentosController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<LancamentoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<LancamentoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Post(
        [FromBody] RegistrarLancamentoRequest request,
        CancellationToken cancellationToken)
    {
        var comercianteIdClaim = User.FindFirstValue("comerciante_id");

        if (comercianteIdClaim is null || !Guid.TryParse(comercianteIdClaim, out var comercianteId))
            return Unauthorized();

        var command = new RegistrarLancamentoCommand(
            comercianteId,
            request.ChaveIdempotencia,
            request.Tipo,
            request.Valor,
            request.DataCompetencia,
            request.Descricao);

        var resultado = await mediator.Send(command, cancellationToken);

        return resultado.Criado
            ? CreatedAtAction(nameof(Post), resultado.Lancamento)
            : Ok(resultado.Lancamento);
    }
}
