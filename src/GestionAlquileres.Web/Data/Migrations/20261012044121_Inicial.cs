using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    DebeCambiarClave = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoIngresoUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UsuarioNombre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Accion = table.Column<int>(type: "int", nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValoresAnteriores = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    ValoresNuevos = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Garantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TipoPersona = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garantes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Impuestos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Impuestos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Indices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Periodicidad = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Indices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inquilinos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TipoPersona = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inquilinos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Numeradores",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UltimoValor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Numeradores", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "Propiedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Calle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Piso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CodigoPostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_Propiedades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Propietarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TipoPersona = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propietarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImpuestosVigencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false),
                    TipoValor = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    VigenciaDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaHasta = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_ImpuestosVigencias", x => x.Id);
                    table.CheckConstraint("CK_ImpuestoVigencia_Vigencia", "[VigenciaHasta] IS NULL OR [VigenciaHasta] >= [VigenciaDesde]");
                    table.ForeignKey(
                        name: "FK_ImpuestosVigencias_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicesValores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndiceId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
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
                    table.PrimaryKey("PK_IndicesValores", x => x.Id);
                    table.CheckConstraint("CK_IndiceValor_Valor", "[Valor] > 0");
                    table.ForeignKey(
                        name: "FK_IndicesValores_Indices_IndiceId",
                        column: x => x.IndiceId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    MontoInicial = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    DiaVencimiento = table.Column<int>(type: "int", nullable: false),
                    DepositoGarantia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaRescision = table.Column<DateOnly>(type: "date", nullable: true),
                    MotivoRescision = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ContratoAnteriorId = table.Column<int>(type: "int", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_Contratos", x => x.Id);
                    table.CheckConstraint("CK_Contrato_DiaVencimiento", "[DiaVencimiento] BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_Contrato_Fechas", "[FechaFin] > [FechaInicio]");
                    table.CheckConstraint("CK_Contrato_MontoInicial", "[MontoInicial] > 0");
                    table.ForeignKey(
                        name: "FK_Contratos_Contratos_ContratoAnteriorId",
                        column: x => x.ContratoAnteriorId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropiedadesPropietarios",
                columns: table => new
                {
                    PropiedadId = table.Column<int>(type: "int", nullable: false),
                    PropietarioId = table.Column<int>(type: "int", nullable: false),
                    Porcentaje = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropiedadesPropietarios", x => new { x.PropiedadId, x.PropietarioId });
                    table.CheckConstraint("CK_PropiedadPropietario_Porcentaje", "[Porcentaje] > 0 AND [Porcentaje] <= 100");
                    table.ForeignKey(
                        name: "FK_PropiedadesPropietarios_Propiedades_PropiedadId",
                        column: x => x.PropiedadId,
                        principalTable: "Propiedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropiedadesPropietarios_Propietarios_PropietarioId",
                        column: x => x.PropietarioId,
                        principalTable: "Propietarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesAjuste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    TipoAjuste = table.Column<int>(type: "int", nullable: false),
                    IndiceId = table.Column<int>(type: "int", nullable: true),
                    PorcentajeFijo = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    PeriodicidadMeses = table.Column<int>(type: "int", nullable: false),
                    ProximaFechaAjuste = table.Column<DateOnly>(type: "date", nullable: false),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesAjuste", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionAjuste_Periodicidad", "[PeriodicidadMeses] BETWEEN 1 AND 60");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAjuste_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesAjuste_Indices_IndiceId",
                        column: x => x.IndiceId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesMora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: true),
                    Criterio = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiasGracia = table.Column<int>(type: "int", nullable: false),
                    VigenciaDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenciaHasta = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_ConfiguracionesMora", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionMora_DiasGracia", "[DiasGracia] >= 0");
                    table.CheckConstraint("CK_ConfiguracionMora_Valor", "[Valor] >= 0");
                    table.CheckConstraint("CK_ConfiguracionMora_Vigencia", "[VigenciaHasta] IS NULL OR [VigenciaHasta] >= [VigenciaDesde]");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesMora_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosGarantes",
                columns: table => new
                {
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    GaranteId = table.Column<int>(type: "int", nullable: false),
                    TipoGarantia = table.Column<int>(type: "int", nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosGarantes", x => new { x.ContratoId, x.GaranteId });
                    table.ForeignKey(
                        name: "FK_ContratosGarantes_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosGarantes_Garantes_GaranteId",
                        column: x => x.GaranteId,
                        principalTable: "Garantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosImpuestos",
                columns: table => new
                {
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    ImpuestoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosImpuestos", x => new { x.ContratoId, x.ImpuestoId });
                    table.ForeignKey(
                        name: "FK_ContratosImpuestos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosImpuestos_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContratosInquilinos",
                columns: table => new
                {
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    InquilinoId = table.Column<int>(type: "int", nullable: false),
                    EsTitular = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContratosInquilinos", x => new { x.ContratoId, x.InquilinoId });
                    table.ForeignKey(
                        name: "FK_ContratosInquilinos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContratosInquilinos_Inquilinos_InquilinoId",
                        column: x => x.InquilinoId,
                        principalTable: "Inquilinos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    Periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuotas_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialMontos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    MontoAnterior = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoNuevo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoAjuste = table.Column<int>(type: "int", nullable: true),
                    IndiceId = table.Column<int>(type: "int", nullable: true),
                    ValorIndiceInicial = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ValorIndiceFinal = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Coeficiente = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AplicadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AplicadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialMontos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialMontos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialMontos_Indices_IndiceId",
                        column: x => x.IndiceId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Moras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    CuotaId = table.Column<int>(type: "int", nullable: false),
                    DiasAtraso = table.Column<int>(type: "int", nullable: false),
                    MontoSugerido = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaAplicacion = table.Column<DateOnly>(type: "date", nullable: false),
                    ConfiguracionMoraId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_Moras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Moras_ConfiguracionesMora_ConfiguracionMoraId",
                        column: x => x.ConfiguracionMoraId,
                        principalTable: "ConfiguracionesMora",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Moras_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Moras_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recibos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<long>(type: "bigint", nullable: false),
                    ContratoId = table.Column<int>(type: "int", nullable: false),
                    CuotaId = table.Column<int>(type: "int", nullable: true),
                    FechaEmision = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: false),
                    Moneda = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MedioPago = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnuladoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnuladoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReemplazadoPorReciboId = table.Column<int>(type: "int", nullable: true),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ModificadoEnUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recibos", x => x.Id);
                    table.CheckConstraint("CK_Recibo_Anulacion", "[Estado] <> 2 OR ([MotivoAnulacion] IS NOT NULL AND [AnuladoEnUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Recibos_Contratos_ContratoId",
                        column: x => x.ContratoId,
                        principalTable: "Contratos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recibos_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recibos_Recibos_ReemplazadoPorReciboId",
                        column: x => x.ReemplazadoPorReciboId,
                        principalTable: "Recibos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecibosItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReciboId = table.Column<int>(type: "int", nullable: false),
                    Concepto = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImpuestoId = table.Column<int>(type: "int", nullable: true),
                    MoraId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecibosItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecibosItems_Impuestos_ImpuestoId",
                        column: x => x.ImpuestoId,
                        principalTable: "Impuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecibosItems_Moras_MoraId",
                        column: x => x.MoraId,
                        principalTable: "Moras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecibosItems_Recibos_ReciboId",
                        column: x => x.ReciboId,
                        principalTable: "Recibos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Entidad_EntidadId",
                table: "AuditLog",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_FechaUtc",
                table: "AuditLog",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UsuarioNombre",
                table: "AuditLog",
                column: "UsuarioNombre");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAjuste_ContratoId",
                table: "ConfiguracionesAjuste",
                column: "ContratoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAjuste_IndiceId",
                table: "ConfiguracionesAjuste",
                column: "IndiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesAjuste_ProximaFechaAjuste",
                table: "ConfiguracionesAjuste",
                column: "ProximaFechaAjuste");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesMora_ContratoId_VigenciaDesde",
                table: "ConfiguracionesMora",
                columns: new[] { "ContratoId", "VigenciaDesde" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_ContratoAnteriorId",
                table: "Contratos",
                column: "ContratoAnteriorId");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_Estado_FechaFin",
                table: "Contratos",
                columns: new[] { "Estado", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_PropiedadId",
                table: "Contratos",
                column: "PropiedadId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosGarantes_GaranteId",
                table: "ContratosGarantes",
                column: "GaranteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosImpuestos_ImpuestoId",
                table: "ContratosImpuestos",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContratosInquilinos_InquilinoId",
                table: "ContratosInquilinos",
                column: "InquilinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_ContratoId_Periodo",
                table: "Cuotas",
                columns: new[] { "ContratoId", "Periodo" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_Estado_FechaVencimiento",
                table: "Cuotas",
                columns: new[] { "Estado", "FechaVencimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_Garantes_Apellido_Nombre",
                table: "Garantes",
                columns: new[] { "Apellido", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Garantes_TipoDocumento_NumeroDocumento",
                table: "Garantes",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialMontos_ContratoId_FechaVigencia",
                table: "HistorialMontos",
                columns: new[] { "ContratoId", "FechaVigencia" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialMontos_IndiceId",
                table: "HistorialMontos",
                column: "IndiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Impuestos_Nombre",
                table: "Impuestos",
                column: "Nombre",
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestosVigencias_ImpuestoId_VigenciaDesde",
                table: "ImpuestosVigencias",
                columns: new[] { "ImpuestoId", "VigenciaDesde" });

            migrationBuilder.CreateIndex(
                name: "IX_Indices_Codigo",
                table: "Indices",
                column: "Codigo",
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_IndicesValores_IndiceId_Fecha",
                table: "IndicesValores",
                columns: new[] { "IndiceId", "Fecha" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_Apellido_Nombre",
                table: "Inquilinos",
                columns: new[] { "Apellido", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Inquilinos_TipoDocumento_NumeroDocumento",
                table: "Inquilinos",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Moras_ConfiguracionMoraId",
                table: "Moras",
                column: "ConfiguracionMoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Moras_ContratoId",
                table: "Moras",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Moras_CuotaId",
                table: "Moras",
                column: "CuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Moras_Estado",
                table: "Moras",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Calle_Numero",
                table: "Propiedades",
                columns: new[] { "Calle", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_PropiedadesPropietarios_PropietarioId",
                table: "PropiedadesPropietarios",
                column: "PropietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Propietarios_Apellido_Nombre",
                table: "Propietarios",
                columns: new[] { "Apellido", "Nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_Propietarios_TipoDocumento_NumeroDocumento",
                table: "Propietarios",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true,
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_ContratoId",
                table: "Recibos",
                column: "ContratoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_CuotaId",
                table: "Recibos",
                column: "CuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_FechaPago",
                table: "Recibos",
                column: "FechaPago");

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_Numero",
                table: "Recibos",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recibos_ReemplazadoPorReciboId",
                table: "Recibos",
                column: "ReemplazadoPorReciboId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosItems_ImpuestoId",
                table: "RecibosItems",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosItems_MoraId",
                table: "RecibosItems",
                column: "MoraId");

            migrationBuilder.CreateIndex(
                name: "IX_RecibosItems_ReciboId",
                table: "RecibosItems",
                column: "ReciboId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "ConfiguracionesAjuste");

            migrationBuilder.DropTable(
                name: "ContratosGarantes");

            migrationBuilder.DropTable(
                name: "ContratosImpuestos");

            migrationBuilder.DropTable(
                name: "ContratosInquilinos");

            migrationBuilder.DropTable(
                name: "HistorialMontos");

            migrationBuilder.DropTable(
                name: "ImpuestosVigencias");

            migrationBuilder.DropTable(
                name: "IndicesValores");

            migrationBuilder.DropTable(
                name: "Numeradores");

            migrationBuilder.DropTable(
                name: "PropiedadesPropietarios");

            migrationBuilder.DropTable(
                name: "RecibosItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Garantes");

            migrationBuilder.DropTable(
                name: "Inquilinos");

            migrationBuilder.DropTable(
                name: "Indices");

            migrationBuilder.DropTable(
                name: "Propietarios");

            migrationBuilder.DropTable(
                name: "Impuestos");

            migrationBuilder.DropTable(
                name: "Moras");

            migrationBuilder.DropTable(
                name: "Recibos");

            migrationBuilder.DropTable(
                name: "ConfiguracionesMora");

            migrationBuilder.DropTable(
                name: "Cuotas");

            migrationBuilder.DropTable(
                name: "Contratos");

            migrationBuilder.DropTable(
                name: "Propiedades");
        }
    }
}
