using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Domain.Entities;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Infrastructure.Repositories;

public sealed class PosicaoDiariaRepository(PosicaoDbContext context) : IPosicaoDiariaRepository
{
    public async Task<PosicaoDiaria> ObterOuCriarAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default)
    {
        var posicao = await context.PosicaoDiaria
            .FirstOrDefaultAsync(p => p.ComercianteId == comercianteId && p.DataPosicao == data, ct);

        if (posicao is not null)
            return posicao;

        posicao = PosicaoDiaria.Criar(comercianteId, data);
        context.PosicaoDiaria.Add(posicao);
        return posicao;
    }
}
