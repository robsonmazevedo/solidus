using Solidus.Posicao.Processor.Domain.Entities;

namespace Solidus.Posicao.Processor.Infrastructure.Repositories;

public interface IPosicaoDiariaRepository
{
    Task<PosicaoDiaria> ObterOuCriarAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default);
}
