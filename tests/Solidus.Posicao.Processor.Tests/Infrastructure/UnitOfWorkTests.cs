using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Domain.Entities;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Tests.Infrastructure;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task CommitAsync_PersisteDadosNoContexto()
    {
        using var ctx = new PosicaoDbContext(
            new DbContextOptionsBuilder<PosicaoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));
        ctx.PosicaoDiaria.Add(posicao);

        var uow = new UnitOfWork(ctx);
        await uow.CommitAsync();

        var salvo = await ctx.PosicaoDiaria.FindAsync(posicao.Id);
        salvo.Should().NotBeNull();
    }
}
