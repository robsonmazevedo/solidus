using System.Text.Json;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Solidus.Contracts.Events;
using Solidus.Registros.API.Infrastructure.Metrics;
using Solidus.Registros.API.Infrastructure.Outbox;
using Solidus.Registros.API.Infrastructure.Persistence;
using Solidus.Registros.API.Infrastructure.Repositories;
using Solidus.Registros.API.Infrastructure.Services;

namespace Solidus.Registros.Tests.Infrastructure;

public sealed class OutboxRelayServiceTests : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IServiceScope _scope = Substitute.For<IServiceScope>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IOutboxRepository _outboxRepo = Substitute.For<IOutboxRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPublishEndpoint _publishEndpoint = Substitute.For<IPublishEndpoint>();
    private readonly RegistrosMetrics _metrics = new();
    private readonly ILogger<OutboxRelayService> _logger = Substitute.For<ILogger<OutboxRelayService>>();

    public OutboxRelayServiceTests()
    {
        _scopeFactory.CreateScope().Returns(_scope);
        _scope.ServiceProvider.Returns(_serviceProvider);
        _serviceProvider.GetService(typeof(IOutboxRepository)).Returns(_outboxRepo);
        _serviceProvider.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);
        _serviceProvider.GetService(typeof(IPublishEndpoint)).Returns(_publishEndpoint);
        _outboxRepo.ObterEstadoPendentesAsync(Arg.Any<CancellationToken>())
            .Returns((0L, (DateTime?)null));
    }

    [Fact]
    public async Task ExecuteAsync_SemPendentes_NaoPublicaEventos()
    {
        _outboxRepo.BuscarPendentesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<OutboxEntry>)Array.Empty<OutboxEntry>());

        var service = new OutboxRelayService(_scopeFactory, _metrics, _logger);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        await _publishEndpoint.DidNotReceive().Publish(Arg.Any<MovimentacaoRegistradaEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ComPendentes_PublicaECommita()
    {
        var evento = new MovimentacaoRegistradaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CREDITO", 100m, DateOnly.FromDateTime(DateTime.UtcNow));
        var entry = OutboxEntry.Criar("MovimentacaoRegistradaEvent", JsonSerializer.Serialize(evento));

        _outboxRepo.BuscarPendentesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                (IReadOnlyList<OutboxEntry>)new List<OutboxEntry> { entry },
                (IReadOnlyList<OutboxEntry>)Array.Empty<OutboxEntry>());

        var service = new OutboxRelayService(_scopeFactory, _metrics, _logger);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(Arg.Any<MovimentacaoRegistradaEvent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ComPendentes_MaisAntigoComValor_AtualizaIdadeMaxima()
    {
        var evento = new MovimentacaoRegistradaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DEBITO", 50m, DateOnly.FromDateTime(DateTime.UtcNow));
        var entry = OutboxEntry.Criar("MovimentacaoRegistradaEvent", JsonSerializer.Serialize(evento));

        _outboxRepo.BuscarPendentesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                (IReadOnlyList<OutboxEntry>)new List<OutboxEntry> { entry },
                (IReadOnlyList<OutboxEntry>)Array.Empty<OutboxEntry>());
        _outboxRepo.ObterEstadoPendentesAsync(Arg.Any<CancellationToken>())
            .Returns((1L, (DateTime?)DateTime.UtcNow.AddMinutes(-2)));

        var service = new OutboxRelayService(_scopeFactory, _metrics, _logger);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        await _publishEndpoint.Received(1).Publish(Arg.Any<MovimentacaoRegistradaEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ErroAoPublicar_LogaEContinua()
    {
        var evento = new MovimentacaoRegistradaEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "CREDITO", 100m, DateOnly.FromDateTime(DateTime.UtcNow));
        var entry = OutboxEntry.Criar("MovimentacaoRegistradaEvent", JsonSerializer.Serialize(evento));

        _outboxRepo.BuscarPendentesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                (IReadOnlyList<OutboxEntry>)new List<OutboxEntry> { entry },
                (IReadOnlyList<OutboxEntry>)Array.Empty<OutboxEntry>());
        _publishEndpoint.Publish(Arg.Any<MovimentacaoRegistradaEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("broker indisponível")));

        var service = new OutboxRelayService(_scopeFactory, _metrics, _logger);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_ErroEmProcessarPendentes_LogaEContinua()
    {
        _outboxRepo.BuscarPendentesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OutboxEntry>>(new InvalidOperationException("db error")));

        var service = new OutboxRelayService(_scopeFactory, _metrics, _logger);
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await service.StopAsync(CancellationToken.None);

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    public void Dispose() => _metrics.Dispose();
}
