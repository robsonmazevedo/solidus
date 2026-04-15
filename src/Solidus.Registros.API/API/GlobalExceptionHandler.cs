using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Solidus.Registros.API.Infrastructure.Metrics;

namespace Solidus.Registros.API.API;

public sealed class GlobalExceptionHandler(RegistrosMetrics metrics) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        metrics.LancamentosErroTotal.Add(1);

        var (status, title) = exception switch
        {
            ArgumentException => (StatusCodes.Status422UnprocessableEntity, "Requisição inválida"),
            _                 => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title  = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
