using ValorVO = Solidus.Registros.API.Domain.ValueObjects.Valor;
using Solidus.Registros.API.Domain.ValueObjects;

namespace Solidus.Registros.API.Domain.Entities;

public sealed class Lancamento
{
    public Guid Id { get; private set; }
    public Guid ComercianteId { get; private set; }
    public string Tipo { get; private set; } = default!;
    public string? Descricao { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly DataCompetencia { get; private set; }
    public string ChaveIdempotencia { get; private set; } = default!;
    public DateTime CriadoEm { get; private set; }

    private Lancamento() { }

    public static Lancamento Registrar(
        Guid comercianteId,
        string tipo,
        decimal valor,
        DateOnly dataCompetencia,
        string chaveIdempotencia,
        string? descricao = null)
    {
        var tipoVO = TipoLancamento.Parse(tipo);
        var valorVO = ValorVO.Criar(valor);

        if (dataCompetencia > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("A data de competência não pode ser futura.");

        return new Lancamento
        {
            Id               = Guid.NewGuid(),
            ComercianteId    = comercianteId,
            Tipo             = tipoVO.ToString(),
            Valor            = valorVO.Quantidade,
            DataCompetencia  = dataCompetencia,
            ChaveIdempotencia = chaveIdempotencia,
            Descricao        = descricao,
            CriadoEm        = DateTime.UtcNow
        };
    }
}
