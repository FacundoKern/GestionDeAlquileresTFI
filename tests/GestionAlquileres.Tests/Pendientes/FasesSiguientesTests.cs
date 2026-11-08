namespace GestionAlquileres.Tests.Pendientes;

/// <summary>
/// Tests acordados para las fases siguientes. Aparecen como "omitidos" en cada ejecución para que no
/// se olviden: al implementar cada funcionalidad, se reemplaza el Skip por el test real (en la carpeta
/// que corresponda: Unidad, Persistencia o Integracion).
/// </summary>
public class FasesSiguientesTests
{
    [Fact(Skip = "Fase 2: AjustesService. Aplicar un ajuste actualiza MontoActual y agrega HistorialMonto en la MISMA transacción; si falla, no cambia ninguno.")]
    public void Aplicar_ajuste_actualiza_monto_e_historial_juntos()
    {
    }

    [Fact(Skip = "Fase 2: cálculo del ajuste por índice (cociente de valores ICL/IPC/Casa Propia), porcentaje fijo y manual, con redondeo a 2 decimales.")]
    public void Calculo_de_ajuste_por_indice_porcentaje_y_manual()
    {
    }

    [Fact(Skip = "Fase 2: contrato en USD con ajuste por ICL muestra advertencia pero se puede guardar.")]
    public void Contrato_en_dolares_con_icl_advierte_sin_bloquear()
    {
    }

    [Fact(Skip = "Fase 3: numeración de recibos correlativa y sin huecos, también con emisiones simultáneas y transacciones revertidas.")]
    public void Numeracion_de_recibos_sin_huecos()
    {
    }

    [Fact(Skip = "Fase 3: pago parcial deja la cuota 'Pagada parcialmente' y el saldo pendiente correcto (a confirmar con el cliente).")]
    public void Pago_parcial_actualiza_el_estado_de_la_cuota()
    {
    }

    [Fact(Skip = "Fase 4: mora sugerida según la configuración vigente a la fecha (monto fijo, porcentaje o interés diario) y días de gracia.")]
    public void Mora_sugerida_segun_configuracion_vigente()
    {
    }

    [Fact(Skip = "Fase 4: impuestos con vigencia: el recibo usa el valor vigente a la fecha del período (a confirmar con el cliente).")]
    public void Impuesto_usa_el_valor_vigente_del_periodo()
    {
    }
}
