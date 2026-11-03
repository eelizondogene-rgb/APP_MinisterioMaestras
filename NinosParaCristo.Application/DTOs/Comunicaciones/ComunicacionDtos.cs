using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Comunicaciones;

public class CrearComunicacionDto
{
    [Required] public int NinoId { get; set; }
    public int? EncargadoId { get; set; }
    [Required, StringLength(200)] public string Asunto { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Mensaje { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Canal { get; set; } = "Registro";
}
