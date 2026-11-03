using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Asistencia;

public class RegistroAsistenciaDto
{
    [Range(1, int.MaxValue)] public int NinoId { get; set; }
    [Required] public bool? Presente { get; set; }
    [StringLength(500)] public string? Observacion { get; set; }
}
public class GuardarAsistenciaDto
{
    [Required] public DateOnly Fecha { get; set; }
    [Required, MinLength(1)] public List<RegistroAsistenciaDto> Registros { get; set; } = [];
}
