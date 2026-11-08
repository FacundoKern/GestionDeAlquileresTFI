namespace GestionAlquileres.Web.ViewModels.Comun;

/// <summary>Página provisoria de una sección que se implementa en una fase siguiente.</summary>
public record EnConstruccionViewModel(string Titulo, string Descripcion, string Fase, IReadOnlyList<string> Pendientes);
