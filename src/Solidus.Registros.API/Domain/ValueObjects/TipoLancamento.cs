namespace Solidus.Registros.API.Domain.ValueObjects;

public sealed record TipoLancamento
{
    public static readonly TipoLancamento Credito = new("CREDITO");
    public static readonly TipoLancamento Debito = new("DEBITO");

    public string Valor { get; }

    private TipoLancamento(string valor) => Valor = valor;

    public static TipoLancamento Parse(string valor) =>
        valor?.ToUpperInvariant() switch
        {
            "CREDITO" => Credito,
            "DEBITO"  => Debito,
            _         => throw new ArgumentException($"Tipo de lançamento inválido: '{valor}'. Use CREDITO ou DEBITO.")
        };

    public override string ToString() => Valor;
}
