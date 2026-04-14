using FluentAssertions;
using Solidus.Registros.API.Domain.Entities;

namespace Solidus.Registros.Tests.Domain;

public sealed class LancamentoTests
{
    private static readonly Guid ComercianteId = Guid.NewGuid();
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Registrar_Credito_CriaLancamentoValido()
    {
        var lancamento = Lancamento.Registrar(ComercianteId, "CREDITO", 100m, Hoje, "chave-1");

        lancamento.Id.Should().NotBeEmpty();
        lancamento.ComercianteId.Should().Be(ComercianteId);
        lancamento.Tipo.Should().Be("CREDITO");
        lancamento.Valor.Should().Be(100m);
        lancamento.DataCompetencia.Should().Be(Hoje);
        lancamento.ChaveIdempotencia.Should().Be("chave-1");
        lancamento.Descricao.Should().BeNull();
    }

    [Fact]
    public void Registrar_Debito_ComDescricao_CriaLancamentoValido()
    {
        var lancamento = Lancamento.Registrar(ComercianteId, "DEBITO", 50m, Hoje, "chave-2", "pag. fornecedor");

        lancamento.Tipo.Should().Be("DEBITO");
        lancamento.Valor.Should().Be(50m);
        lancamento.Descricao.Should().Be("pag. fornecedor");
    }

    [Fact]
    public void Registrar_DataFutura_LancaArgumentException()
    {
        var amanha = Hoje.AddDays(1);
        var act = () => Lancamento.Registrar(ComercianteId, "CREDITO", 100m, amanha, "chave-3");
        act.Should().Throw<ArgumentException>().WithMessage("*futura*");
    }

    [Fact]
    public void Registrar_ValorZero_LancaArgumentException()
    {
        var act = () => Lancamento.Registrar(ComercianteId, "CREDITO", 0m, Hoje, "chave-4");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Registrar_ValorNegativo_LancaArgumentException()
    {
        var act = () => Lancamento.Registrar(ComercianteId, "CREDITO", -10m, Hoje, "chave-5");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Registrar_TipoInvalido_LancaArgumentException()
    {
        var act = () => Lancamento.Registrar(ComercianteId, "INVALIDO", 100m, Hoje, "chave-6");
        act.Should().Throw<ArgumentException>();
    }
}
