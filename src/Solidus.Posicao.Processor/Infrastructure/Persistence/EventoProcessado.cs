namespace Solidus.Posicao.Processor.Infrastructure.Persistence;

public sealed class EventoProcessado
{
    private EventoProcessado() { }

    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public string TipoEvento { get; private set; } = default!;
    public DateTime ProcessadoEm { get; private set; }

    public static EventoProcessado Registrar(Guid eventoId, string tipoEvento) => new()
    {
        Id            = Guid.NewGuid(),
        EventoId      = eventoId,
        TipoEvento    = tipoEvento,
        ProcessadoEm  = DateTime.UtcNow
    };
}
