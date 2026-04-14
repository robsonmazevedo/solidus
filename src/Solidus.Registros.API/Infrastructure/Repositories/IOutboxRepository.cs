using Solidus.Registros.API.Infrastructure.Outbox;

namespace Solidus.Registros.API.Infrastructure.Repositories;

public interface IOutboxRepository
{
    Task AdicionarAsync(OutboxEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxEntry>> BuscarPendentesAsync(int limite, CancellationToken cancellationToken = default);
    Task<(long Pendentes, DateTime? MaisAntigo)> ObterEstadoPendentesAsync(CancellationToken cancellationToken = default);
    void MarcarPublicado(OutboxEntry entry);
}
