using MediatR;
using Solidus.Registros.API.Application.DTOs;

namespace Solidus.Registros.API.Application.Commands;

public sealed record RegistrarLancamentoCommand(
    Guid ComercianteId,
    string ChaveIdempotencia,
    string Tipo,
    decimal Valor,
    DateOnly DataCompetencia,
    string? Descricao) : IRequest<RegistrarLancamentoResult>;
