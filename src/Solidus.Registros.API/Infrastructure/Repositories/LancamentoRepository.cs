using Microsoft.EntityFrameworkCore;
using Solidus.Registros.API.Domain.Entities;
using Solidus.Registros.API.Infrastructure.Persistence;

namespace Solidus.Registros.API.Infrastructure.Repositories;

public sealed class LancamentoRepository(RegistrosDbContext context) : ILancamentoRepository
{
    public Task<Lancamento?> BuscarPorChaveIdempotenciaAsync(
        string chaveIdempotencia,
        CancellationToken cancellationToken = default)
        => context.Lancamentos
            .FirstOrDefaultAsync(l => l.ChaveIdempotencia == chaveIdempotencia, cancellationToken);

    public async Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default)
        => await context.Lancamentos.AddAsync(lancamento, cancellationToken);
}
