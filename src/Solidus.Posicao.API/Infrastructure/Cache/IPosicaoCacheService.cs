using Solidus.Posicao.API.Application.DTOs;

namespace Solidus.Posicao.API.Infrastructure.Cache;

public interface IPosicaoCacheService
{
    Task<PosicaoDiariaDto?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default);
    Task GravarAsync(Guid comercianteId, DateOnly data, PosicaoDiariaDto dto, CancellationToken ct = default);
}
