using System.ComponentModel.DataAnnotations;

namespace NinosParaCristo.Application.DTOs.Calendarios;

public class GuardarGrupoClaseDto
{
    [Required, StringLength(120)] public string Nombre { get; set; } = string.Empty;
    [Range(0, 18)] public int EdadMinima { get; set; }
    [Range(0, 18)] public int EdadMaxima { get; set; }
    [Range(0, 100)] public int Orden { get; set; }
}

public class GuardarAsignacionClaseMensualDto
{
    [Range(2020, 2100)] public int Anio { get; set; }
    [Range(1, 12)] public int Mes { get; set; }
    [Required] public int GrupoClaseId { get; set; }
    [Required] public int MaestraId { get; set; }
    [StringLength(200)] public string? Tema { get; set; }
    [StringLength(1000)] public string? MaterialUrl { get; set; }
    [StringLength(1000)] public string? Observaciones { get; set; }
}
