using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Solidus.Registros.API.API;
using Solidus.Registros.API.Infrastructure.HealthChecks;
using Solidus.Registros.API.Infrastructure.Persistence;
using Solidus.Registros.API.Infrastructure.Repositories;
using Solidus.Registros.API.Infrastructure.Metrics;
using Solidus.Registros.API.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RegistrosDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Registros")));

builder.Services.AddScoped<ILancamentoRepository, LancamentoRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:User"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });

        cfg.Message<Solidus.Contracts.Events.MovimentacaoRegistradaEvent>(m =>
            m.SetEntityName("movimentacao-registrada"));
    });
});

builder.Services.AddSingleton<RegistrosMetrics>();
builder.Services.AddHostedService<OutboxRelayService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidateAudience         = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ValidateLifetime         = true
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("por-comerciante", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirstValue("comerciante_id")
                ?? ctx.Connection.RemoteIpAddress?.ToString()
                ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 100),
                Window      = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("registros-api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MassTransit")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();
        if (otlpEndpoint is not null)
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(RegistrosMetrics.MeterName)
        .AddPrometheusExporter());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistrosDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    ctx.Response.Headers.XFrameOptions = "DENY";
    ctx.Response.Headers.StrictTransportSecurity = "max-age=31536000";
    await next(ctx);
});

app.Use(async (ctx, next) =>
{
    var metrics = ctx.RequestServices.GetRequiredService<RegistrosMetrics>();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next(ctx);
    metrics.HttpDuracaoSegundos.Record(sw.Elapsed.TotalSeconds,
        new KeyValuePair<string, object?>("method", ctx.Request.Method),
        new KeyValuePair<string, object?>("route", ctx.GetEndpoint()?.DisplayName ?? "unknown"),
        new KeyValuePair<string, object?>("status_code", ctx.Response.StatusCode));
});

app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => Results.Redirect("/scalar/v1")).AllowAnonymous();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();

await app.RunAsync();
