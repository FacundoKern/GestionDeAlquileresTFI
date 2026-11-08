using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GestionAlquileres.Web.Infrastructure.TagHelpers;

/// <summary>
/// Marca el enlace de la sección actual con la clase "active" y aria-current="page"
/// (el lector de pantalla anuncia "página actual"; no depende solo del color).
/// <list type="bullet">
/// <item>ga-seccion="Inquilinos": activo en ese controlador del área raíz.</item>
/// <item>ga-seccion="Usuarios,Auditoria" ga-area="Administracion": activo en cualquiera de esos controladores del área.</item>
/// <item>ga-area="Administracion" (sin ga-seccion): activo en toda el área.</item>
/// </list>
/// </summary>
[HtmlTargetElement("a", Attributes = "ga-seccion")]
[HtmlTargetElement("a", Attributes = "ga-area")]
public class NavegacionTagHelper : TagHelper
{
    [HtmlAttributeName("ga-seccion")]
    public string? Seccion { get; set; }

    [HtmlAttributeName("ga-area")]
    public string? Area { get; set; }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var valores = ViewContext.RouteData.Values;
        var areaActual = valores["area"]?.ToString() ?? string.Empty;
        var controladorActual = valores["controller"]?.ToString() ?? string.Empty;

        var mismaArea = string.Equals(areaActual, Area ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        var activo = mismaArea && (Seccion is null || Seccion
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(controladorActual, StringComparer.OrdinalIgnoreCase));

        if (!activo)
        {
            return;
        }

        output.AddClass("active", HtmlEncoder.Default);
        output.Attributes.SetAttribute("aria-current", "page");
    }
}
