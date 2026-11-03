namespace NinosParaCristo.Domain.Entities;

public class ComunicacionPadre
{
    public int Id { get; set; }
    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;
    public int? EncargadoId { get; set; }
    public Encargado? Encargado { get; set; }
    public string Asunto { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Canal { get; set; } = "Registro";
    public string? Destinatario { get; set; }
    public string Estado { get; set; } = "Registrado";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaEnvio { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
