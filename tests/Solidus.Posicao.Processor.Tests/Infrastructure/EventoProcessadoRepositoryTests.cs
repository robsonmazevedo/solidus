using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Infrastructure.Persistence;
using Solidus.Posicao.Processor.Infrastructure.Repositories;

namespace Solidus.Posicao.Processor.Tests.Infrastructure;

public sealed class EventoProcessadoRepositoryTests
{
    private static PosicaoDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<PosicaoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task ExisteAsync_EventoExistente_RetornaTrue()
    {
        using var ctx = CriarContexto();
        var eventoId = Guid.NewGuid();
        ctx.EventosProcessados.Add(EventoProcessado.Registrar(eventoId, "MovimentacaoRegistrada"));
        await ctx.SaveChangesAsync();

        var repo = new EventoProcessadoRepository(ctx);
        var resultado = await repo.ExisteAsync(eventoId);

        resultado.Should().BeTrue();
    }

    [Fact]
    public async Task ExisteAsync_EventoInexistente_RetornaFalse()
    {
        using var ctx = CriarContexto();
        var repo = new EventoProcessadoRepository(ctx);

        var resultado = await repo.ExisteAsync(Guid.NewGuid());

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task AdicionarAsync_AdicionaEventoNoContexto()
    {
        using var ctx = CriarContexto();
        var evento = EventoProcessado.Registrar(Guid.NewGuid(), "MovimentacaoRegistrada");
        var repo = new EventoProcessadoRepository(ctx);

        await repo.AdicionarAsync(evento);

        ctx.EventosProcessados.Local.Should().Contain(evento);
    }
}
