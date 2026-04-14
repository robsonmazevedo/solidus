using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solidus.Posicao.Processor.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "posicao");

            migrationBuilder.CreateTable(
                name: "eventos_processados",
                schema: "posicao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento = table.Column<string>(type: "varchar(100)", nullable: false),
                    processado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_eventos_processados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "posicao_diaria",
                schema: "posicao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    comerciante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_posicao = table.Column<DateOnly>(type: "date", nullable: false),
                    total_creditos = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValueSql: "0"),
                    total_debitos = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValueSql: "0"),
                    saldo = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValueSql: "0"),
                    atualizado_em = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_posicao_diaria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_eventos_processados_evento_id",
                schema: "posicao",
                table: "eventos_processados",
                column: "evento_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_posicao_diaria_comerciante_id_data_posicao",
                schema: "posicao",
                table: "posicao_diaria",
                columns: new[] { "comerciante_id", "data_posicao" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eventos_processados",
                schema: "posicao");

            migrationBuilder.DropTable(
                name: "posicao_diaria",
                schema: "posicao");
        }
    }
}
