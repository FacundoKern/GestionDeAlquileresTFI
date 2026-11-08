using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatosEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatosEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreComercial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cuit = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    MatriculaCorredor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CodigoPostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Whatsapp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SitioWeb = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HorarioAtencion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoporteNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoporteTelefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoporteEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosEmpresa", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatosEmpresa");
        }
    }
}
