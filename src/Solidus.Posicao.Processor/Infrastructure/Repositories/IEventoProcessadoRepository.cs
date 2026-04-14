using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Infrastructure.Repositories;

public interface IEventoProcessadoRepository
{
    Task<bool> ExisteAsync(Guid eventoId, CancellationToken ct = default);
    Task AdicionarAsync(EventoProcessado evento, CancellationToken ct = default);
}
