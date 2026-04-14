using FluentAssertions;
using Solidus.Registros.API.Domain.ValueObjects;

namespace Solidus.Registros.Tests.Domain;

public sealed class ValorTests
{
    [Fact]
    public void Criar_ValorPositivo_RetornaInstanciaCorreta()
    {
        var valor = Valor.Criar(100m);
        valor.Quantidade.Should().Be(100m);
    }

    [Fact]
    public void Criar_ValorZero_LancaArgumentException()
    {
        var act = () => Valor.Criar(0m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_ValorNegativo_LancaArgumentException()
    {
        var act = () => Valor.Criar(-1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Criar_ArredondaParaDuasCasasDecimais()
    {
        var valor = Valor.Criar(10.555m);
        valor.Quantidade.Should().Be(10.56m);
    }

    [Fact]
    public void ConversaoImplicita_RetornaDecimalEquivalente()
    {
        var valor = Valor.Criar(50m);
        decimal resultado = valor;
        resultado.Should().Be(50m);
    }

    [Fact]
    public void ToString_RetornaFormatoF2()
    {
        var valor = Valor.Criar(10m);
        valor.ToString().Should().Be(10m.ToString("F2"));
    }
}
