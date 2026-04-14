namespace Solidus.Contracts.Events;

public sealed record MovimentacaoRegistradaEvent(
    Guid EventoId,
    Guid LancamentoId,
    Guid ComercianteId,
    string Tipo,
    decimal Valor,
    DateOnly DataCompetencia);
