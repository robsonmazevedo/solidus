using MediatR;

namespace Solidus.Posicao.Processor.Application.Commands;

public sealed record ProcessarMovimentacaoCommand(
    Guid EventoId,
    Guid LancamentoId,
    Guid ComercianteId,
    string Tipo,
    decimal Valor,
    DateOnly DataCompetencia) : IRequest;
