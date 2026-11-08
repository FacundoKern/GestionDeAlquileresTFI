using GestionAlquileres.Web.ViewModels.Comun;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Areas.Administracion.Controllers;

// Secciones de Administración que se implementan en fases siguientes. Ver CLAUDE.md, "Plan de fases".

// TODO (fase 2): ABM de contratos.
public class ContratosController : AdministracionControllerBase
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Contratos",
        "Alta, edición, renovación y rescisión de contratos.",
        "fase 2",
        [
            "Alta con propiedad, inquilinos (titular y coinquilinos), garantes y tipo de garantía.",
            "Configuración de ajuste (índice, porcentaje fijo o manual) y periodicidad.",
            "Advertencia (sin bloquear) si un contrato en USD se ajusta por ICL.",
            "Advertencia si el contrato no tiene garante (regla a confirmar con el cliente).",
            "Ficha del contrato, solo para Administrador por ahora.",
            "Rescisión con fecha y motivo; renovación como contrato nuevo vinculado al anterior.",
            "Generación de cuotas mensuales.",
        ]));
}

// TODO (fase 2): aplicar ajustes de precio.
public class AjustesController : AdministracionControllerBase
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Ajustes de precio",
        "Contratos con ajuste próximo o vencido, con el cálculo sugerido para revisar y aplicar.",
        "fase 2",
        [
            "Cálculo por índice (cociente de valores), porcentaje fijo o monto manual.",
            "Vista previa del nuevo monto antes de aplicar.",
            "Al aplicar: HistorialMonto + MontoActual en la misma transacción.",
            "PDF de actualización de contrato.",
        ]));
}

// TODO (fase 4): bandeja de mora.
public class MoraController : AdministracionControllerBase
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Bandeja de mora",
        "Contratos con cuotas atrasadas para revisar y aplicar mora manualmente.",
        "fase 4",
        [
            "Listado de cuotas vencidas impagas con días de atraso.",
            "Monto sugerido según la configuración de mora vigente (general o del contrato).",
            "Aplicación manual con monto final y motivo; condonación.",
            "PDF de aviso de mora.",
        ]));
}

// TODO (fase 2): carga de valores de índices.
public class IndicesController : AdministracionControllerBase
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Índices",
        "Carga manual de valores de ICL, IPC, Casa Propia y otros índices.",
        "fase 2",
        [
            "Listado de valores por índice y fecha.",
            "Alta y corrección de valores (auditadas).",
            "Alta de nuevos índices.",
        ]));
}

// TODO (fase 4): impuestos y configuración de mora.
public class ImpuestosMoraController : AdministracionControllerBase
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Impuestos y mora",
        "Valores con vigencia: modificar crea una nueva vigencia, no pisa la anterior.",
        "fase 4",
        [
            "Impuestos/conceptos: alta, nuevas vigencias y asignación a contratos (pendiente de confirmar con el cliente).",
            "Configuración general de mora: criterio, valor, días de gracia y vigencia.",
            "Configuración de mora particular por contrato.",
        ]));
}
