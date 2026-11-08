using GestionAlquileres.Web.Data;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Tests.Infraestructura;

/// <summary>Una base efímera compartida por los tests de persistencia (cada test crea sus propios datos).</summary>
public sealed class BaseDeDatosFixture : IAsyncLifetime
{
    public BaseDeDatosEfimera Base { get; } = new("Persistencia");

    public Task InitializeAsync() => Base.CrearAsync();

    public Task DisposeAsync() => Base.BorrarAsync();

    public AppDbContext CrearContexto(Web.Security.IUsuarioActual? usuario = null) => Base.CrearContexto(usuario);
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionBaseDeDatos : ICollectionFixture<BaseDeDatosFixture>
{
    public const string Nombre = "Base de datos";
}

/// <summary>Datos mínimos válidos para los tests. Los documentos son únicos para que los tests no choquen entre sí.</summary>
public static class Crear
{
    private static int _secuencia = 10_000_000;

    public static string DniUnico() => Interlocked.Increment(ref _secuencia).ToString();

    public static Propietario Propietario(string? dni = null, string nombre = "Ana", string apellido = "Prueba") => new()
    {
        TipoPersona = TipoPersona.Fisica,
        Nombre = nombre,
        Apellido = apellido,
        TipoDocumento = TipoDocumento.Dni,
        NumeroDocumento = dni ?? DniUnico(),
    };

    public static Propiedad Propiedad() => new()
    {
        Calle = "Calle de Prueba",
        Numero = "123",
        Localidad = "Palermo",
        Provincia = "Ciudad Autónoma de Buenos Aires",
        Tipo = TipoPropiedad.Departamento,
    };

    /// <summary>Contrato vigente con su propiedad y el monto inicial registrado en el historial.</summary>
    public static Contrato Contrato(decimal montoInicial = 500_000m)
    {
        var contrato = new Contrato
        {
            Propiedad = Propiedad(),
            FechaInicio = new DateOnly(2026, 1, 1),
            FechaFin = new DateOnly(2028, 12, 31),
            Moneda = Moneda.Ars,
            DiaVencimiento = 10,
        };
        contrato.EstablecerMontoInicial(montoInicial, UsuarioDePrueba.Operador.Nombre, DateTime.UtcNow);
        return contrato;
    }

    public static Recibo Recibo(Contrato contrato, decimal total = 500_000m) => new()
    {
        Numero = Interlocked.Increment(ref _secuencia),
        Contrato = contrato,
        FechaEmision = new DateOnly(2026, 2, 10),
        FechaPago = new DateOnly(2026, 2, 10),
        Moneda = Moneda.Ars,
        Total = total,
        MedioPago = MedioPago.Efectivo,
    };
}
