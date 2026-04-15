using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Solidus.Posicao.Processor.Infrastructure.Consumers;
using Solidus.Posicao.Processor.Infrastructure.Metrics;
using Solidus.Posicao.Processor.Infrastructure.HealthChecks;
using Solidus.Contracts.Events;
using Solidus.Posicao.Processor.Infrastructure.Persistence;
using Solidus.Posicao.Processor.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<PosicaoDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Posicao")));

builder.Services.AddScoped<IPosicaoDiariaRepository, PosicaoDiariaRepository>();
builder.Services.AddScoped<IEventoProcessadoRepository, EventoProcessadoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<ProcessorMetrics>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MovimentacaoRegistradaConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:User"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.Message<MovimentacaoRegistradaEvent>(m =>
            m.SetEntityName("movimentacao-registrada"));

        cfg.ReceiveEndpoint("posicao-processor", e =>
        {
            e.ConfigureConsumer<MovimentacaoRegistradaConsumer>(ctx);
        });
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("posicao-processor"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MassTransit")
            .AddEntityFrameworkCoreInstrumentation();
        if (otlpEndpoint is not null)
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics => metrics
        .AddMeter(ProcessorMetrics.MeterName)
        .AddPrometheusHttpListener(o =>
            o.UriPrefixes = new[] { builder.Configuration["Prometheus:Endpoint"]! }));

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PosicaoDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
