using Microsoft.EntityFrameworkCore;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Infrastructure.Repositories;

public sealed class EventoProcessadoRepository(PosicaoDbContext context) : IEventoProcessadoRepository
{
    public Task<bool> ExisteAsync(Guid eventoId, CancellationToken ct = default)
        => context.EventosProcessados.AnyAsync(e => e.EventoId == eventoId, ct);

    public Task AdicionarAsync(EventoProcessado evento, CancellationToken ct = default)
    {
        context.EventosProcessados.Add(evento);
        return Task.CompletedTask;
    }
}
