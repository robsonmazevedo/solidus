namespace Solidus.Registros.API.Application.DTOs;

public sealed record RegistrarLancamentoRequest(
    string ChaveIdempotencia,
    string Tipo,
    decimal Valor,
    DateOnly DataCompetencia,
    string? Descricao);
