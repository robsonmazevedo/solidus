namespace Solidus.Posicao.API.Domain.ReadModels;

public sealed class PosicaoDiaria
{
    public Guid Id { get; private set; }
    public Guid ComercianteId { get; private set; }
    public DateOnly DataPosicao { get; private set; }
    public decimal TotalCreditos { get; private set; }
    public decimal TotalDebitos { get; private set; }
    public decimal Saldo { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
}
