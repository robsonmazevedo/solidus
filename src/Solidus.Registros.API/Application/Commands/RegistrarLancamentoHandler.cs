using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Solidus.Registros.API.Application.DTOs;
using Solidus.Registros.API.Domain.Entities;
using Solidus.Contracts.Events;
using Solidus.Registros.API.Infrastructure.Metrics;
using Solidus.Registros.API.Infrastructure.Outbox;
using Solidus.Registros.API.Infrastructure.Persistence;
using Solidus.Registros.API.Infrastructure.Repositories;

namespace Solidus.Registros.API.Application.Commands;

public sealed class RegistrarLancamentoHandler(
    ILancamentoRepository lancamentoRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    RegistrosMetrics metrics) : IRequestHandler<RegistrarLancamentoCommand, RegistrarLancamentoResult>
{
    public async Task<RegistrarLancamentoResult> Handle(
        RegistrarLancamentoCommand command,
        CancellationToken cancellationToken)
    {
        var existente = await lancamentoRepository
            .BuscarPorChaveIdempotenciaAsync(command.ChaveIdempotencia, cancellationToken);

        if (existente is not null)
            return new RegistrarLancamentoResult(ToDto(existente), Criado: false);

        var lancamento = Lancamento.Registrar(
            command.ComercianteId,
            command.Tipo,
            command.Valor,
            command.DataCompetencia,
            command.ChaveIdempotencia,
            command.Descricao);

        var evento = new MovimentacaoRegistradaEvent(
            EventoId:        Guid.NewGuid(),
            LancamentoId:    lancamento.Id,
            ComercianteId:   lancamento.ComercianteId,
            Tipo:            lancamento.Tipo,
            Valor:           lancamento.Valor,
            DataCompetencia: lancamento.DataCompetencia);

        var outbox = OutboxEntry.Criar(
            nameof(MovimentacaoRegistradaEvent),
            JsonSerializer.Serialize(evento));

        try
        {
            await lancamentoRepository.AdicionarAsync(lancamento, cancellationToken);
            await outboxRepository.AdicionarAsync(outbox, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            metrics.LancamentosTotal.Add(1);
            return new RegistrarLancamentoResult(ToDto(lancamento), Criado: true);
        }
        catch (DbUpdateException)
        {
            var duplicado = await lancamentoRepository
                .BuscarPorChaveIdempotenciaAsync(command.ChaveIdempotencia, cancellationToken);
            return new RegistrarLancamentoResult(ToDto(duplicado!), Criado: false);
        }
    }

    private static LancamentoDto ToDto(Lancamento l) => new(
        l.Id,
        l.ComercianteId,
        l.Tipo,
        l.Valor,
        l.DataCompetencia,
        l.ChaveIdempotencia,
        l.Descricao,
        l.CriadoEm);
}
