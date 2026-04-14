namespace Solidus.Registros.API.Application.DTOs;

public sealed record LancamentoDto(
    Guid Id,
    Guid ComercianteId,
    string Tipo,
    decimal Valor,
    DateOnly DataCompetencia,
    string ChaveIdempotencia,
    string? Descricao,
    DateTime CriadoEm);
