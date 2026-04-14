using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Solidus.Posicao.API.Application.Queries;

namespace Solidus.Posicao.API.API.Controllers;

[ApiController]
[Route("posicao")]
[Authorize]
[EnableRateLimiting("por-comerciante")]
public sealed class PosicaoController(IMediator mediator) : ControllerBase
{
    [HttpGet("diaria")]
    public async Task<IActionResult> ConsultarDiaria(
        [FromQuery] DateOnly data,
        CancellationToken ct)
    {
        if (data > DateOnly.FromDateTime(DateTime.UtcNow))
            return UnprocessableEntity(new { erro = "A data de consulta não pode ser futura." });

        var comercianteId = Guid.Parse(User.FindFirstValue("comerciante_id")!);
        var query = new ConsultarPosicaoDiariaQuery(comercianteId, data);
        var result = await mediator.Send(query, ct);

        return Ok(result);
    }
}
