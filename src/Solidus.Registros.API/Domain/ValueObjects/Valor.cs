namespace Solidus.Registros.API.Domain.ValueObjects;

public sealed record Valor
{
    public decimal Quantidade { get; }

    private Valor(decimal quantidade) => Quantidade = quantidade;

    public static Valor Criar(decimal quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("O valor da movimentação deve ser maior que zero.");

        return new Valor(Math.Round(quantidade, 2));
    }

    public static implicit operator decimal(Valor valor) => valor.Quantidade;

    public override string ToString() => Quantidade.ToString("F2");
}
