using Microsoft.EntityFrameworkCore;
using Solidus.Registros.API.Domain.Entities;
using Solidus.Registros.API.Infrastructure.Outbox;

namespace Solidus.Registros.API.Infrastructure.Persistence;

public sealed class RegistrosDbContext(DbContextOptions<RegistrosDbContext> options) : DbContext(options)
{
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<OutboxEntry> Outbox => Set<OutboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("registros");

        modelBuilder.Entity<Lancamento>(entity =>
        {
            entity.ToTable("lancamentos", t => t.HasCheckConstraint("ck_lancamentos_valor", "valor > 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.ComercianteId).HasColumnName("comerciante_id").HasColumnType("uuid").IsRequired();
            entity.Property(e => e.Tipo).HasColumnName("tipo").HasColumnType("varchar(7)").IsRequired();
            entity.Property(e => e.Descricao).HasColumnName("descricao").HasColumnType("varchar(255)");
            entity.Property(e => e.Valor).HasColumnName("valor").HasColumnType("numeric(18,2)").IsRequired();
            entity.Property(e => e.DataCompetencia).HasColumnName("data_competencia").IsRequired();
            entity.Property(e => e.ChaveIdempotencia).HasColumnName("chave_idempotencia").HasColumnType("varchar(64)").IsRequired();
            entity.Property(e => e.CriadoEm).HasColumnName("criado_em").HasColumnType("timestamptz").IsRequired()
                .HasDefaultValueSql("now()").ValueGeneratedOnAdd();

            entity.HasIndex(e => e.ChaveIdempotencia).IsUnique();
            entity.HasIndex(e => new { e.ComercianteId, e.DataCompetencia });
        });

        modelBuilder.Entity<OutboxEntry>(entity =>
        {
            entity.ToTable("outbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid");
            entity.Property(e => e.TipoEvento).HasColumnName("tipo_evento").HasColumnType("varchar(100)").IsRequired();
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasColumnType("varchar(10)").IsRequired()
                .HasDefaultValueSql("'PENDENTE'");
            entity.Property(e => e.CriadoEm).HasColumnName("criado_em").HasColumnType("timestamptz").IsRequired()
                .HasDefaultValueSql("now()").ValueGeneratedOnAdd();
            entity.Property(e => e.PublicadoEm).HasColumnName("publicado_em").HasColumnType("timestamptz");

            entity.HasIndex(e => new { e.Status, e.CriadoEm });
        });
    }
}
