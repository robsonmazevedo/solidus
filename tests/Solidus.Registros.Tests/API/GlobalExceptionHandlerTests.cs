using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Solidus.Registros.API.API;
using Solidus.Registros.API.Infrastructure.Metrics;

namespace Solidus.Registros.Tests.API;

public sealed class GlobalExceptionHandlerTests
{
    private static DefaultHttpContext CriarContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task TryHandleAsync_ArgumentException_Retorna422()
    {
        var handler = new GlobalExceptionHandler(new RegistrosMetrics());
        var context = CriarContext();

        var result = await handler.TryHandleAsync(context, new ArgumentException("valor inválido"), CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task TryHandleAsync_ExcecaoGenerica_Retorna500()
    {
        var handler = new GlobalExceptionHandler(new RegistrosMetrics());
        var context = CriarContext();

        var result = await handler.TryHandleAsync(context, new InvalidOperationException("erro interno"), CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
    }
}
