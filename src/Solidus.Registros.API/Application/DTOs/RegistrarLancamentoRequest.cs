using System.Text.Json.Serialization;

namespace Solidus.Registros.API.Application.DTOs;

public sealed record RegistrarLancamentoRequest(
    string ChaveIdempotencia,
    string Tipo,
    [property: JsonRequired]
    decimal Valor,
    [property: JsonRequired]
    DateOnly DataCompetencia,
    string? Descricao);
