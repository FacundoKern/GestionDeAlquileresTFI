using System.ComponentModel.DataAnnotations;

namespace GestionAlquileres.Web.Models.Enums;

public enum TipoPropiedad
{
    [Display(Name = "Departamento")] Departamento = 1,
    [Display(Name = "Casa")] Casa = 2,
    [Display(Name = "PH")] Ph = 3,
    [Display(Name = "Local comercial")] Local = 4,
    [Display(Name = "Oficina")] Oficina = 5,
    [Display(Name = "Cochera")] Cochera = 6,
    [Display(Name = "Galpón / depósito")] Galpon = 7,
    [Display(Name = "Terreno")] Terreno = 8,
    [Display(Name = "Otro")] Otro = 99,
}

/// <summary>
/// Estado calculado de una propiedad (no se guarda): "Alquilada" si tiene un contrato vigente,
/// "Inactiva" si se la marcó como inactiva, "Disponible" en otro caso.
/// </summary>
public enum EstadoPropiedad
{
    [Display(Name = "Disponible")] Disponible = 1,
    [Display(Name = "Alquilada")] Alquilada = 2,
    [Display(Name = "Inactiva")] Inactiva = 3,
}
