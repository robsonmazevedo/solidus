using FluentAssertions;
using Solidus.Posicao.Processor.Domain.Entities;

namespace Solidus.Posicao.Processor.Tests.Domain;

public sealed class PosicaoDiariaTests
{
    [Fact]
    public void Criar_InicializaPropriedadesCorretamente()
    {
        var comercianteId = Guid.NewGuid();
        var data = new DateOnly(2024, 1, 15);

        var posicao = PosicaoDiaria.Criar(comercianteId, data);

        posicao.Id.Should().NotBe(Guid.Empty);
        posicao.ComercianteId.Should().Be(comercianteId);
        posicao.DataPosicao.Should().Be(data);
        posicao.TotalCreditos.Should().Be(0m);
        posicao.TotalDebitos.Should().Be(0m);
        posicao.Saldo.Should().Be(0m);
    }

    [Fact]
    public void AplicarMovimentacao_Credito_IncrementaTotalCreditosESaldo()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        posicao.AplicarMovimentacao("CREDITO", 100m);

        posicao.TotalCreditos.Should().Be(100m);
        posicao.TotalDebitos.Should().Be(0m);
        posicao.Saldo.Should().Be(100m);
    }

    [Fact]
    public void AplicarMovimentacao_Debito_IncrementaTotalDebitosEDecrementaSaldo()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        posicao.AplicarMovimentacao("DEBITO", 50m);

        posicao.TotalCreditos.Should().Be(0m);
        posicao.TotalDebitos.Should().Be(50m);
        posicao.Saldo.Should().Be(-50m);
    }

    [Fact]
    public void AplicarMovimentacao_MultiplasMov_CalculaSaldoCorretamente()
    {
        var posicao = PosicaoDiaria.Criar(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        posicao.AplicarMovimentacao("CREDITO", 200m);
        posicao.AplicarMovimentacao("DEBITO", 80m);
        posicao.AplicarMovimentacao("CREDITO", 50m);

        posicao.TotalCreditos.Should().Be(250m);
        posicao.TotalDebitos.Should().Be(80m);
        posicao.Saldo.Should().Be(170m);
    }
}
