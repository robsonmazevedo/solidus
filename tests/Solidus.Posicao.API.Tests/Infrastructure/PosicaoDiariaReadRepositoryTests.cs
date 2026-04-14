using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.API.Domain.ReadModels;
using Solidus.Posicao.API.Infrastructure.Persistence;
using Solidus.Posicao.API.Infrastructure.Repositories;

namespace Solidus.Posicao.API.Tests.Infrastructure;

public sealed class PosicaoDiariaReadRepositoryTests
{
    private static PosicaoReadDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<PosicaoReadDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PosicaoReadDbContext(options);
    }

    private static PosicaoDiaria CriarPosicaoDiaria(Guid comercianteId, DateOnly data)
    {
        var posicao = new PosicaoDiaria();
        var type = typeof(PosicaoDiaria);
        type.GetProperty(nameof(PosicaoDiaria.Id))!.SetValue(posicao, Guid.NewGuid());
        type.GetProperty(nameof(PosicaoDiaria.ComercianteId))!.SetValue(posicao, comercianteId);
        type.GetProperty(nameof(PosicaoDiaria.DataPosicao))!.SetValue(posicao, data);
        type.GetProperty(nameof(PosicaoDiaria.TotalCreditos))!.SetValue(posicao, 100m);
        type.GetProperty(nameof(PosicaoDiaria.TotalDebitos))!.SetValue(posicao, 40m);
        type.GetProperty(nameof(PosicaoDiaria.Saldo))!.SetValue(posicao, 60m);
        type.GetProperty(nameof(PosicaoDiaria.AtualizadoEm))!.SetValue(posicao, DateTime.UtcNow);
        return posicao;
    }

    [Fact]
    public async Task ObterAsync_PosicaoExistente_RetornaPosicao()
    {
        var comercianteId = Guid.NewGuid();
        var data = DateOnly.FromDateTime(DateTime.UtcNow);
        await using var context = CriarContexto();
        context.PosicaoDiaria.Add(CriarPosicaoDiaria(comercianteId, data));
        await context.SaveChangesAsync();

        var repository = new PosicaoDiariaReadRepository(context);
        var result = await repository.ObterAsync(comercianteId, data);

        result.Should().NotBeNull();
        result!.ComercianteId.Should().Be(comercianteId);
        result.DataPosicao.Should().Be(data);
    }

    [Fact]
    public async Task ObterAsync_PosicaoInexistente_RetornaNull()
    {
        await using var context = CriarContexto();
        var repository = new PosicaoDiariaReadRepository(context);

        var result = await repository.ObterAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow));

        result.Should().BeNull();
    }
}
