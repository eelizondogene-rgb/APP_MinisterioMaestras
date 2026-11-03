using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Protocolo;

public class GuardarProtocoloDto
{
    [Required] public DateOnly Fecha { get; set; }
    public int? ResponsableMeriendaId { get; set; }
    public int? ResponsableProtocoloId { get; set; }
    [StringLength(300)] public string? Merienda { get; set; }
    [StringLength(1000)] public string? Tareas { get; set; }
}
public class ActualizarBitacoraDto
{
    [StringLength(2000)] public string? Bitacora { get; set; }
    public bool Completado { get; set; }
}
