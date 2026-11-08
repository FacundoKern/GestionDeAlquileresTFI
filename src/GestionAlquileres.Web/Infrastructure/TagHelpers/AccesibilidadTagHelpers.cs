using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace GestionAlquileres.Web.Infrastructure.TagHelpers;

/// <summary>
/// Vincula cada campo con su mensaje de error (aria-describedby) y marca aria-invalid cuando el
/// servidor devolvió errores, para que los lectores de pantalla anuncien el problema al enfocar el campo.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
[HtmlTargetElement("select", Attributes = "asp-for")]
[HtmlTargetElement("textarea", Attributes = "asp-for")]
public class CampoAccesibleTagHelper : TagHelper
{
    // Se ejecuta después de los tag helpers de ASP.NET Core (que generan id y name).
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.TryGetAttribute("type", out var tipo) && tipo.Value?.ToString() == "hidden")
        {
            return;
        }

        var nombre = NombresCampo.NombreCompleto(ViewContext, For);
        var idError = NombresCampo.IdError(nombre);

        var descritoPor = output.Attributes.TryGetAttribute("aria-describedby", out var existente)
            ? $"{existente.Value} {idError}"
            : idError;
        output.Attributes.SetAttribute("aria-describedby", descritoPor);

        if (ViewContext.ViewData.ModelState.TryGetValue(nombre, out var estado) && estado.Errors.Count > 0)
        {
            output.Attributes.SetAttribute("aria-invalid", "true");
        }
    }
}

/// <summary>Da al mensaje de error un id estable para que el campo lo referencie.</summary>
[HtmlTargetElement("span", Attributes = "asp-validation-for")]
public class MensajeValidacionAccesibleTagHelper : TagHelper
{
    public override int Order => 1000;

    [HtmlAttributeName("asp-validation-for")]
    public ModelExpression For { get; set; } = null!;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.SetAttribute("id", NombresCampo.IdError(NombresCampo.NombreCompleto(ViewContext, For)));
    }
}

/// <summary>
/// Agrega el texto "(obligatorio)" a la etiqueta de los campos requeridos: la obligatoriedad no se
/// comunica solo con un asterisco o un color.
/// </summary>
[HtmlTargetElement("label", Attributes = "asp-for")]
public class EtiquetaObligatoriaTagHelper : TagHelper
{
    public override int Order => 1000;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var metadata = For.Metadata;
        var esCasilla = metadata.UnderlyingOrModelType == typeof(bool);
        if (metadata.IsRequired && !esCasilla)
        {
            output.PostContent.AppendHtml(" <span class=\"texto-obligatorio\">(obligatorio)</span>");
        }
    }
}

internal static class NombresCampo
{
    public static string NombreCompleto(ViewContext contexto, ModelExpression expresion) =>
        contexto.ViewData.TemplateInfo.GetFullHtmlFieldName(expresion.Name);

    public static string IdError(string nombreCompleto) =>
        TagBuilder.CreateSanitizedId(nombreCompleto, "_") + "-error";
}
