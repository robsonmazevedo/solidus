using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Scalar.AspNetCore;
using Solidus.Posicao.API.API;
using Solidus.Posicao.API.Infrastructure.Metrics;
using Solidus.Posicao.API.Infrastructure.Cache;
using Solidus.Posicao.API.Infrastructure.HealthChecks;
using Solidus.Posicao.API.Infrastructure.Persistence;
using Solidus.Posicao.API.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PosicaoReadDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Posicao")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddScoped<IPosicaoDiariaReadRepository, PosicaoDiariaReadRepository>();
builder.Services.AddSingleton<PosicaoMetrics>();
builder.Services.AddScoped<IPosicaoCacheService, PosicaoCacheService>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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
                PermitLimit = builder.Configuration.GetValue<int>("RateLimit:PermitLimit", 200),
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
    .ConfigureResource(r => r.AddService("posicao-api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();
        if (otlpEndpoint is not null)
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    })
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(PosicaoMetrics.MeterName)
        .AddPrometheusExporter());

var app = builder.Build();

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
    var metrics = ctx.RequestServices.GetRequiredService<PosicaoMetrics>();
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

app.Run();
