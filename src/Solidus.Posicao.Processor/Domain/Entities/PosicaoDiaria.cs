namespace Solidus.Posicao.Processor.Domain.Entities;

public sealed class PosicaoDiaria
{
    public Guid Id { get; private set; }
    public Guid ComercianteId { get; private set; }
    public DateOnly DataPosicao { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal Saldo { get; private set; }
    public DateTime AtualizadoEm { get; private set; }

    private PosicaoDiaria() { }

    public static PosicaoDiaria Criar(Guid comercianteId, DateOnly dataPosicao) => new()
    {
        Id            = Guid.NewGuid(),
        ComercianteId = comercianteId,
        DataPosicao   = dataPosicao,
        TotalCreditos = 0,
        TotalDebitos  = 0,
        Saldo         = 0,
        AtualizadoEm  = DateTime.UtcNow
    };

    public void AplicarMovimentacao(string tipo, decimal valor)
    {
        if (tipo == "CREDITO")
            TotalCreditos += valor;
        else
            TotalDebitos += valor;

        Saldo        = TotalCreditos - TotalDebitos;
        AtualizadoEm = DateTime.UtcNow;
    }
}
