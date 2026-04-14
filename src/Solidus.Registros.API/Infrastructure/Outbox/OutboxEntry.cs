namespace Solidus.Registros.API.Infrastructure.Outbox;

public sealed class OutboxEntry
{
    public Guid Id { get; private set; }
    public string TipoEvento { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string Status { get; private set; } = "PENDENTE";
    public DateTime CriadoEm { get; private set; }
    public DateTime? PublicadoEm { get; private set; }

    private OutboxEntry() { }

    public static OutboxEntry Criar(string tipoEvento, string payload) => new()
    {
        Id         = Guid.NewGuid(),
        TipoEvento = tipoEvento,
        Payload    = payload,
        Status     = "PENDENTE",
        CriadoEm  = DateTime.UtcNow
    };

    public void MarcarPublicado()
    {
        Status      = "PUBLICADO";
        PublicadoEm = DateTime.UtcNow;
    }
}
