using Microsoft.EntityFrameworkCore;
using Solidus.Registros.API.Infrastructure.Outbox;
using Solidus.Registros.API.Infrastructure.Persistence;

namespace Solidus.Registros.API.Infrastructure.Repositories;

public sealed class OutboxRepository(RegistrosDbContext context) : IOutboxRepository
{
    public async Task AdicionarAsync(OutboxEntry entry, CancellationToken cancellationToken = default)
        => await context.Outbox.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<OutboxEntry>> BuscarPendentesAsync(
        int limite,
        CancellationToken cancellationToken = default)
        => await context.Outbox
            .Where(e => e.Status == "PENDENTE")
            .OrderBy(e => e.CriadoEm)
            .Take(limite)
            .ToListAsync(cancellationToken);

    public async Task<(long Pendentes, DateTime? MaisAntigo)> ObterEstadoPendentesAsync(
        CancellationToken cancellationToken = default)
    {
        var pendentes = await context.Outbox
            .Where(e => e.Status == "PENDENTE")
            .ToListAsync(cancellationToken);

        return (pendentes.Count, pendentes.Min(e => (DateTime?)e.CriadoEm));
    }

    public void MarcarPublicado(OutboxEntry entry)
        => entry.MarcarPublicado();
}
