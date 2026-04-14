using System.Text.Json;
using StackExchange.Redis;
using Solidus.Posicao.API.Application.DTOs;

namespace Solidus.Posicao.API.Infrastructure.Cache;

public sealed class PosicaoCacheService(IConnectionMultiplexer redis) : IPosicaoCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<PosicaoDiariaDto?> ObterAsync(Guid comercianteId, DateOnly data, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(CacheKey(comercianteId, data));

        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<PosicaoDiariaDto>((string)value!, JsonOptions);
    }

    public async Task GravarAsync(Guid comercianteId, DateOnly data, PosicaoDiariaDto dto, CancellationToken ct = default)
    {
        var db = redis.GetDatabase();
        var ttl = data == DateOnly.FromDateTime(DateTime.UtcNow)
            ? TimeSpan.FromSeconds(30)
            : TimeSpan.FromHours(1);

        await db.StringSetAsync(
            CacheKey(comercianteId, data),
            JsonSerializer.Serialize(dto, JsonOptions),
            ttl);
    }

    private static string CacheKey(Guid comercianteId, DateOnly data)
        => $"posicao:{comercianteId}:{data:yyyy-MM-dd}";
}
