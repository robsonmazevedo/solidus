using FluentAssertions;
using Solidus.Registros.API.Infrastructure.Outbox;

namespace Solidus.Registros.Tests.Infrastructure;

public sealed class OutboxEntryTests
{
    [Fact]
    public void Criar_InicializaComStatusPendente()
    {
        var entry = OutboxEntry.Criar("MovimentacaoRegistradaEvent", "{\"id\":\"1\"}");

        entry.Id.Should().NotBeEmpty();
        entry.TipoEvento.Should().Be("MovimentacaoRegistradaEvent");
        entry.Payload.Should().Be("{\"id\":\"1\"}");
        entry.Status.Should().Be("PENDENTE");
        entry.PublicadoEm.Should().BeNull();
    }

    [Fact]
    public void MarcarPublicado_AlteraStatusParaPublicado()
    {
        var entry = OutboxEntry.Criar("MovimentacaoRegistradaEvent", "{}");

        entry.MarcarPublicado();

        entry.Status.Should().Be("PUBLICADO");
        entry.PublicadoEm.Should().NotBeNull();
    }
}
