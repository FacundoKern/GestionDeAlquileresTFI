using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.Models.Enums;

// Los valores numéricos son explícitos porque se guardan en la base: no reordenar ni reutilizar.

public enum TipoPersona
{
    [Display(Name = "Persona física")] Fisica = 1,
    [Display(Name = "Persona jurídica")] Juridica = 2,
}

public enum TipoDocumento
{
    [Display(Name = "DNI")] Dni = 1,
    [Display(Name = "CUIT")] Cuit = 2,
    [Display(Name = "CUIL")] Cuil = 3,
    [Display(Name = "Pasaporte")] Pasaporte = 4,
}
