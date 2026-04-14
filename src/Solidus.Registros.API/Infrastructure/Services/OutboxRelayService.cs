using System.Text.Json;
using MassTransit;
using Solidus.Contracts.Events;
using Solidus.Registros.API.Infrastructure.Persistence;
using Solidus.Registros.API.Infrastructure.Repositories;

namespace Solidus.Registros.API.Infrastructure.Services;

public sealed class OutboxRelayService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(5);
    private const int LoteMaximo = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessarPendentesAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Erro ao processar outbox pendente");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }

    private async Task ProcessarPendentesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var outboxRepository  = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork        = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var publishEndpoint   = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pendentes = await outboxRepository.BuscarPendentesAsync(LoteMaximo, cancellationToken);

        if (pendentes.Count == 0)
            return;

        foreach (var entry in pendentes)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<MovimentacaoRegistradaEvent>(entry.Payload)!;
                await publishEndpoint.Publish(evento, cancellationToken);
                outboxRepository.MarcarPublicado(entry);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao publicar evento {EventoId}", entry.Id);
            }
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
