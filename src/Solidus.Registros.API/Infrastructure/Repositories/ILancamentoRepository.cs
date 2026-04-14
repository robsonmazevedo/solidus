using Solidus.Registros.API.Domain.Entities;

namespace Solidus.Registros.API.Infrastructure.Repositories;

public interface ILancamentoRepository
{
    Task<Lancamento?> BuscarPorChaveIdempotenciaAsync(
        string chaveIdempotencia,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(Lancamento lancamento, CancellationToken cancellationToken = default);
}
