using FluentAssertions;
using Solidus.Registros.API.Domain.ValueObjects;

namespace Solidus.Registros.Tests.Domain;

public sealed class TipoLancamentoTests
{
    [Fact]
    public void Parse_Credito_RetornaTipoCredito()
    {
        TipoLancamento.Parse("CREDITO").Should().Be(TipoLancamento.Credito);
    }

    [Fact]
    public void Parse_Debito_RetornaTipoDebito()
    {
        TipoLancamento.Parse("DEBITO").Should().Be(TipoLancamento.Debito);
    }

    [Theory]
    [InlineData("credito")]
    [InlineData("Credito")]
    [InlineData("CREDITO")]
    public void Parse_CaseInsensitive_RetornaCredito(string valor)
    {
        TipoLancamento.Parse(valor).Should().Be(TipoLancamento.Credito);
    }

    [Theory]
    [InlineData("debito")]
    [InlineData("Debito")]
    [InlineData("DEBITO")]
    public void Parse_CaseInsensitive_RetornaDebito(string valor)
    {
        TipoLancamento.Parse(valor).Should().Be(TipoLancamento.Debito);
    }

    [Fact]
    public void Parse_ValorInvalido_LancaArgumentException()
    {
        var act = () => TipoLancamento.Parse("TRANSFERENCIA");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Parse_Null_LancaArgumentException()
    {
        var act = () => TipoLancamento.Parse(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_RetornaValorString()
    {
        TipoLancamento.Credito.ToString().Should().Be("CREDITO");
        TipoLancamento.Debito.ToString().Should().Be("DEBITO");
    }
}
