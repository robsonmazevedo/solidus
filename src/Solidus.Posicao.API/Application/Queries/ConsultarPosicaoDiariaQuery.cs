using MediatR;
using Solidus.Posicao.API.Application.DTOs;

namespace Solidus.Posicao.API.Application.Queries;

public sealed record ConsultarPosicaoDiariaQuery(
    Guid ComercianteId,
    DateOnly Data) : IRequest<PosicaoDiariaDto>;
