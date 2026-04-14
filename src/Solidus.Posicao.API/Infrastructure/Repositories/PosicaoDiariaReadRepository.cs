using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.API.Domain.ReadModels;
using Solidus.Posicao.API.Infrastructure.Persistence;

namespace Solidus.Posicao.API.Infrastructure.Repositories;

public sealed class PosicaoDiariaReadRepository(PosicaoReadDbContext context) : IPosicaoDiariaReadRepository
{
    public Task<PosicaoDiaria?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default)
        => context.PosicaoDiaria
            .FirstOrDefaultAsync(p => p.ComercianteId == comercianteId && p.DataPosicao == data, ct);
}
