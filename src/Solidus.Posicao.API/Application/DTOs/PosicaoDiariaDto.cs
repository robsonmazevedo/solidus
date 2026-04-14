namespace Solidus.Posicao.API.Application.DTOs;

public sealed record PosicaoDiariaDto(
    DateOnly Data,
    decimal TotalCreditos,
    decimal TotalDebitos,
    decimal Saldo,
    DateTime AtualizadoEm);
