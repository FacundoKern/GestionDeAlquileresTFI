using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class FotosPropiedades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FotosPropiedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    Archivo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Ancho = table.Column<int>(type: "int", nullable: false),
                    Alto = table.Column<int>(type: "int", nullable: false),
                    TamanioBytes = table.Column<long>(type: "bigint", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosPropiedades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosPropiedades_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosPropiedades_Archivo",
                table: "FotosPropiedades",
                column: "Archivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FotosPropiedades_PropiedadId_Orden",
                table: "FotosPropiedades",
                columns: new[] { "PropiedadId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosPropiedades");
        }
    }
}
