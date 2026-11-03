using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Turnos;

public class GuardarTurnoClaseDto
{
    [Required] public DateOnly Fecha { get; set; }
    [Required] public int MaestraId { get; set; }
    [StringLength(200)] public string? Tema { get; set; }
    [StringLength(1000)] public string? MaterialUrl { get; set; }
    [StringLength(1000)] public string? Observaciones { get; set; }
}
