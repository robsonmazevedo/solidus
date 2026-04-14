using FluentAssertions;
using Solidus.Posicao.Processor.Infrastructure.Persistence;

namespace Solidus.Posicao.Processor.Tests.Infrastructure.Persistence;

public sealed class EventoProcessadoTests
{
    [Fact]
    public void Registrar_InicializaPropriedadesCorretamente()
    {
        var eventoId = Guid.NewGuid();
        const string tipoEvento = "MovimentacaoRegistrada";
        var antes = DateTime.UtcNow;

        var evento = EventoProcessado.Registrar(eventoId, tipoEvento);

        evento.Id.Should().NotBe(Guid.Empty);
        evento.EventoId.Should().Be(eventoId);
        evento.TipoEvento.Should().Be(tipoEvento);
        evento.ProcessadoEm.Should().BeOnOrAfter(antes);
    }
}
