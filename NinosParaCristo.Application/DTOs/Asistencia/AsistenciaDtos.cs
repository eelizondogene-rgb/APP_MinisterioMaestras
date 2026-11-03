using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Asistencia;

public class RegistroAsistenciaDto
{
    public int NinoId { get; set; }
    public bool Presente { get; set; }
    [StringLength(500)] public string? Observacion { get; set; }
}
public class GuardarAsistenciaDto
{
    [Required] public DateOnly Fecha { get; set; }
    [MinLength(1)] public List<RegistroAsistenciaDto> Registros { get; set; } = [];
}
