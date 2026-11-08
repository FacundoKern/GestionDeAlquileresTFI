using GestionAlquileres.Tests.Infraestructura;
using GestionAlquileres.Web.Models.Entidades;
using GestionAlquileres.Web.Models.Enums;

namespace GestionAlquileres.Tests.Unidad;

/// <summary>Regla: MontoActual cambia solo junto con una entrada nueva de HistorialMonto.</summary>
public class MontoContratoTests
{
    [Fact]
    public void El_monto_inicial_queda_registrado_en_el_historial()
    {
        var contrato = Crear.Contrato(montoInicial: 500_000m);

        Assert.Equal(500_000m, contrato.MontoInicial);
        Assert.Equal(500_000m, contrato.MontoActual);
        var inicial = Assert.Single(contrato.HistorialMontos);
        Assert.Equal(0m, inicial.MontoAnterior);
        Assert.Equal(500_000m, inicial.MontoNuevo);
        Assert.Null(inicial.TipoAjuste);
    }

    [Fact]
    public void Un_nuevo_monto_agrega_historial_con_el_monto_anterior()
    {
        var contrato = Crear.Contrato(montoInicial: 500_000m);

        contrato.RegistrarNuevoMonto(new HistorialMonto
        {
            MontoNuevo = 550_000m,
            FechaVigencia = new DateOnly(2027, 1, 1),
            TipoAjuste = TipoAjuste.PorcentajeFijo,
            AplicadoPor = "tests",
        });

        Assert.Equal(550_000m, contrato.MontoActual);
        Assert.Equal(500_000m, contrato.MontoInicial);
        Assert.Equal(2, contrato.HistorialMontos.Count);
        Assert.Equal(500_000m, contrato.HistorialMontos.Last().MontoAnterior);
    }

    [Fact]
    public void El_monto_inicial_no_se_puede_fijar_dos_veces()
    {
        var contrato = Crear.Contrato();

        Assert.Throws<InvalidOperationException>(() => contrato.EstablecerMontoInicial(1m, "tests", DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Un_monto_no_positivo_se_rechaza_sin_cambiar_nada(decimal monto)
    {
        var contrato = Crear.Contrato(montoInicial: 500_000m);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            contrato.RegistrarNuevoMonto(new HistorialMonto { MontoNuevo = monto, AplicadoPor = "tests" }));

        Assert.Equal(500_000m, contrato.MontoActual);
        Assert.Single(contrato.HistorialMontos);
    }
}
