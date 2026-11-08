using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace GestionAlquileres.Web.Infrastructure.Configuracion;

public static class LocalizacionExtensions
{
    /// <summary>
    /// Fija la cultura es-AR para toda la aplicación (no se negocia con el navegador):
    /// así el model binding interpreta siempre "1.234,56" y las fechas dd/MM/yyyy.
    /// </summary>
    public static IServiceCollection AgregarLocalizacionArgentina(this IServiceCollection services)
    {
        CultureInfo.DefaultThreadCurrentCulture = CulturaArgentina.Cultura;
        CultureInfo.DefaultThreadCurrentUICulture = CulturaArgentina.Cultura;

        services.Configure<RequestLocalizationOptions>(opciones =>
        {
            opciones.DefaultRequestCulture = new RequestCulture(CulturaArgentina.Cultura);
            opciones.SupportedCultures = [CulturaArgentina.Cultura];
            opciones.SupportedUICultures = [CulturaArgentina.Cultura];
            opciones.RequestCultureProviders.Clear();
        });

        return services;
    }

    /// <summary>Mensajes de model binding en español (también se usan en la validación de cliente).</summary>
    public static void TraducirMensajesDeBinding(DefaultModelBindingMessageProvider proveedor)
    {
        proveedor.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor «{valor}» no es válido para {campo}.");
        proveedor.SetMissingBindRequiredValueAccessor(campo => $"Falta un valor para {campo}.");
        proveedor.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
        proveedor.SetMissingRequestBodyRequiredValueAccessor(() => "Falta el contenido de la solicitud.");
        proveedor.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
        proveedor.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
        proveedor.SetNonPropertyValueMustBeANumberAccessor(() => "El campo debe ser un número.");
        proveedor.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado no es válido para {campo}.");
        proveedor.SetValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
        proveedor.SetValueMustBeANumberAccessor(campo => $"{campo} debe ser un número.");
        proveedor.SetValueMustNotBeNullAccessor(campo => $"{campo} es obligatorio.");
    }
}
