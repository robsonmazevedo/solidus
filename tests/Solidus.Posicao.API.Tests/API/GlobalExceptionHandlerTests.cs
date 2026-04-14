using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Solidus.Posicao.API.API;

namespace Solidus.Posicao.API.Tests.API;

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
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var context = CriarContext();

        var result = await handler.TryHandleAsync(context, new ArgumentException("valor inválido"), CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task TryHandleAsync_ExcecaoGenerica_Retorna500()
    {
        var logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
        var handler = new GlobalExceptionHandler(logger);
        var context = CriarContext();

        var result = await handler.TryHandleAsync(context, new InvalidOperationException("erro"), CancellationToken.None);

        result.Should().BeTrue();
        context.Response.StatusCode.Should().Be(500);
    }
}
