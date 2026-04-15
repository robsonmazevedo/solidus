using MediatR;
using Solidus.Posicao.API.Application.DTOs;
using Solidus.Posicao.API.Application.Queries;
using Solidus.Posicao.API.Infrastructure.Cache;
using Solidus.Posicao.API.Infrastructure.Metrics;
using Solidus.Posicao.API.Infrastructure.Repositories;

namespace Solidus.Posicao.API.Application.Handlers;

public sealed class ConsultarPosicaoDiariaHandler(
    IPosicaoCacheService cache,
    IPosicaoDiariaReadRepository repository,
    PosicaoMetrics metrics) : IRequestHandler<ConsultarPosicaoDiariaQuery, PosicaoDiariaDto>
{
    public async Task<PosicaoDiariaDto> Handle(ConsultarPosicaoDiariaQuery query, CancellationToken cancellationToken)
    {
        metrics.ConsultasTotal.Add(1);

        var cached = await cache.ObterAsync(query.ComercianteId, query.Data, cancellationToken);
        if (cached is not null)
        {
            metrics.CacheHitTotal.Add(1);
            return cached;
        }

        metrics.CacheMissTotal.Add(1);

        var posicao = await repository.ObterAsync(query.ComercianteId, query.Data, cancellationToken);

        if (posicao is null)
            return new PosicaoDiariaDto(query.Data, 0, 0, 0, DateTime.UtcNow);

        var dto = new PosicaoDiariaDto(
            posicao.DataPosicao,
            posicao.TotalCreditos,
            posicao.TotalDebitos,
            posicao.Saldo,
            posicao.AtualizadoEm);

        await cache.GravarAsync(query.ComercianteId, query.Data, dto, cancellationToken);

        return dto;
    }
}
