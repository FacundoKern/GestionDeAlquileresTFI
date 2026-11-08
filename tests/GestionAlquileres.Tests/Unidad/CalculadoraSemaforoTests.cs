using GestionAlquileres.Web.Infrastructure.Configuracion;
using GestionAlquileres.Web.Models.Enums;
using GestionAlquileres.Web.Services.Contratos;
using GestionAlquileres.Web.ViewModels.Comun;

namespace GestionAlquileres.Tests.Unidad;

public class CalculadoraSemaforoTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 14);
    private static readonly SemaforoOptions Opciones = new() { DiasContratoPorVencer = 60, DiasPagoProximo = 5 };

    private static SemaforoViewModel Calcular(
        EstadoContrato estado = EstadoContrato.Vigente,
        DateOnly? fin = null,
        bool mora = false,
        bool cuotaVencida = false,
        DateOnly? proximoVencimiento = null) =>
        CalculadoraSemaforo.Calcular(
            new DatosSemaforoContrato(estado, fin ?? Hoy.AddYears(1), mora, cuotaVencida, proximoVencimiento),
            Hoy,
            Opciones).Semaforo;

    [Fact]
    public void Contrato_sin_novedades_esta_al_dia()
    {
        var semaforo = Calcular(proximoVencimiento: Hoy.AddDays(20));

        Assert.Equal(NivelSemaforo.AlDia, semaforo.Nivel);
        Assert.Equal("Al día", semaforo.Texto);
    }

    [Theory]
    [InlineData(EstadoContrato.Rescindido, "Rescindido")]
    [InlineData(EstadoContrato.Finalizado, "Finalizado")]
    public void Rescindido_y_finalizado_son_neutros_aunque_tengan_deuda_o_fecha_pasada(EstadoContrato estado, string texto)
    {
        var semaforo = Calcular(estado, fin: Hoy.AddDays(-30), mora: true, cuotaVencida: true);

        Assert.Equal(NivelSemaforo.Neutro, semaforo.Nivel);
        Assert.Equal(texto, semaforo.Texto);
    }

    [Fact]
    public void Vigente_con_fecha_de_fin_pasada_esta_vencido_y_tiene_prioridad_sobre_la_mora()
    {
        var semaforo = Calcular(fin: Hoy.AddDays(-1), mora: true);

        Assert.Equal(NivelSemaforo.Critico, semaforo.Nivel);
        Assert.Equal("Vencido", semaforo.Texto);
    }

    [Fact]
    public void El_dia_que_termina_todavia_no_esta_vencido()
    {
        var semaforo = Calcular(fin: Hoy);

        Assert.Equal("Por vencer", semaforo.Texto);
    }

    [Fact]
    public void Mora_activa_es_critica_y_tiene_prioridad_sobre_pago_atrasado_y_por_vencer()
    {
        var semaforo = Calcular(fin: Hoy.AddDays(10), mora: true, cuotaVencida: true);

        Assert.Equal(NivelSemaforo.Critico, semaforo.Nivel);
        Assert.Equal("En mora", semaforo.Texto);
    }

    [Fact]
    public void Cuota_vencida_impaga_es_pago_atrasado()
    {
        var semaforo = Calcular(cuotaVencida: true);

        Assert.Equal(NivelSemaforo.Critico, semaforo.Nivel);
        Assert.Equal("Pago atrasado", semaforo.Texto);
    }

    [Theory]
    [InlineData(60, "Por vencer")]
    [InlineData(61, "Al día")]
    public void Por_vencer_usa_el_umbral_configurado(int diasHastaElFin, string esperado)
    {
        Assert.Equal(esperado, Calcular(fin: Hoy.AddDays(diasHastaElFin)).Texto);
    }

    [Theory]
    [InlineData(0, "Pago próximo")]
    [InlineData(5, "Pago próximo")]
    [InlineData(6, "Al día")]
    public void Pago_proximo_usa_el_umbral_configurado(int diasHastaElVencimiento, string esperado)
    {
        Assert.Equal(esperado, Calcular(proximoVencimiento: Hoy.AddDays(diasHastaElVencimiento)).Texto);
    }

    [Fact]
    public void Por_vencer_tiene_prioridad_sobre_pago_proximo()
    {
        var semaforo = Calcular(fin: Hoy.AddDays(30), proximoVencimiento: Hoy.AddDays(2));

        Assert.Equal(NivelSemaforo.Atencion, semaforo.Nivel);
        Assert.Equal("Por vencer", semaforo.Texto);
    }

    [Fact]
    public void Los_umbrales_se_toman_de_la_configuracion()
    {
        var opciones = new SemaforoOptions { DiasContratoPorVencer = 90, DiasPagoProximo = 10 };
        var datos = new DatosSemaforoContrato(EstadoContrato.Vigente, Hoy.AddDays(80), false, false, null);

        Assert.Equal("Por vencer", CalculadoraSemaforo.Calcular(datos, Hoy, opciones).Semaforo.Texto);
    }
}
