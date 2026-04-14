using Solidus.Posicao.API.Domain.ReadModels;

namespace Solidus.Posicao.API.Infrastructure.Repositories;

public interface IPosicaoDiariaReadRepository
{
    Task<PosicaoDiaria?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default);
}
