using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solidus.Registros.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registros");

            migrationBuilder.CreateTable(
                name: "lancamentos",
                schema: "registros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comerciante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "varchar(7)", nullable: false),
                    descricao = table.Column<string>(type: "varchar(255)", nullable: true),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    chave_idempotencia = table.Column<string>(type: "varchar(64)", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos", x => x.id);
                    table.CheckConstraint("ck_lancamentos_valor", "valor > 0");
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "registros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento = table.Column<string>(type: "varchar(100)", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "varchar(10)", nullable: false, defaultValueSql: "'PENDENTE'"),
                    criado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    publicado_em = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_chave_idempotencia",
                schema: "registros",
                table: "lancamentos",
                column: "chave_idempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lancamentos_comerciante_id_data_competencia",
                schema: "registros",
                table: "lancamentos",
                columns: new[] { "comerciante_id", "data_competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_status_criado_em",
                schema: "registros",
                table: "outbox",
                columns: new[] { "status", "criado_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lancamentos",
                schema: "registros");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "registros");
        }
    }
}
