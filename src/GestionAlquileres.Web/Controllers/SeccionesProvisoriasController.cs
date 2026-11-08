using GestionAlquileres.Web.Security;
using GestionAlquileres.Web.ViewModels.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.Web.Controllers;

// TODO (fase 3): registro de pagos y emisión/anulación de recibos. Ver CLAUDE.md, "Plan de fases".
[Authorize(Policy = Politicas.Gestion)]
public class PagosController : Controller
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Pagos",
        "Registro de pagos de cuotas y emisión de recibos.",
        "fase 3",
        [
            "Cuotas pendientes por contrato y registro de pagos totales o parciales.",
            "Emisión de recibos con numeración correlativa sin huecos.",
            "Anulación de recibos con motivo (solo Administrador) y emisión del recibo que lo reemplaza.",
            "PDF del recibo.",
        ]));
}

// TODO (fase 5): reportes con exportación a PDF (QuestPDF) y Excel (ClosedXML).
[Authorize(Policy = Politicas.Lectura)]
public class ReportesController : Controller
{
    public IActionResult Index() => View("EnConstruccion", new EnConstruccionViewModel(
        "Reportes",
        "Selector de reportes con exportación a PDF y Excel.",
        "fase 5",
        [
            "Contratos de un propietario.",
            "Inquilinos de una propiedad.",
            "Pagos de un período (agrupados por moneda).",
            "Moras aplicadas y pendientes.",
        ]));
}
