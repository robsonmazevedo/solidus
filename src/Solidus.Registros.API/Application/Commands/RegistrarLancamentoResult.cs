using Solidus.Registros.API.Application.DTOs;

namespace Solidus.Registros.API.Application.Commands;

public sealed record RegistrarLancamentoResult(LancamentoDto Lancamento, bool Criado);
