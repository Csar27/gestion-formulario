using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionFormulario.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RespuestaValorHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RespuestaValorHistoriales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RespuestaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValorAnterior = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ValorAnteriorTexto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ValorNuevo = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ValorNuevoTexto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPorId = table.Column<string>(type: "text", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModificadoPorId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestaValorHistoriales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RespuestaValorHistoriales_Campos_CampoId",
                        column: x => x.CampoId,
                        principalTable: "Campos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestaValorHistoriales_Respuestas_RespuestaId",
                        column: x => x.RespuestaId,
                        principalTable: "Respuestas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaValorHistoriales_CampoId",
                table: "RespuestaValorHistoriales",
                column: "CampoId");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestaValorHistoriales_RespuestaId",
                table: "RespuestaValorHistoriales",
                column: "RespuestaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RespuestaValorHistoriales");
        }
    }
}
