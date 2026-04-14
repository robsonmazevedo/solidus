using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Domain.Entities;
using Solidus.Posicao.Processor.Infrastructure.Persistence;
using Solidus.Posicao.Processor.Infrastructure.Repositories;

namespace Solidus.Posicao.Processor.Tests.Infrastructure;

public sealed class PosicaoDiariaRepositoryTests
{
    private static PosicaoDbContext CriarContexto() =>
        new(new DbContextOptionsBuilder<PosicaoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task ObterOuCriarAsync_RegistroExistente_RetornaRegistroExistente()
    {
        using var ctx = CriarContexto();
        var comercianteId = Guid.NewGuid();
        var data = new DateOnly(2024, 6, 1);
        var posicaoExistente = PosicaoDiaria.Criar(comercianteId, data);
        ctx.PosicaoDiaria.Add(posicaoExistente);
        await ctx.SaveChangesAsync();

        var repo = new PosicaoDiariaRepository(ctx);
        var resultado = await repo.ObterOuCriarAsync(comercianteId, data);

        resultado.Id.Should().Be(posicaoExistente.Id);
    }

    [Fact]
    public async Task ObterOuCriarAsync_RegistroInexistente_CriaERetornaNovoRegistro()
    {
        using var ctx = CriarContexto();
        var comercianteId = Guid.NewGuid();
        var data = new DateOnly(2024, 6, 2);

        var repo = new PosicaoDiariaRepository(ctx);
        var resultado = await repo.ObterOuCriarAsync(comercianteId, data);

        resultado.Id.Should().NotBe(Guid.Empty);
        resultado.ComercianteId.Should().Be(comercianteId);
        resultado.DataPosicao.Should().Be(data);
        ctx.PosicaoDiaria.Local.Should().Contain(resultado);
    }
}
