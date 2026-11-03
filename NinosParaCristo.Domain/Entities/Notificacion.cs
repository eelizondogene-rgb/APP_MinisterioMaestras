namespace NinosParaCristo.Domain.Entities;

public class Notificacion
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Tipo { get; set; } = "General";
    public DateTime FechaProgramada { get; set; } = DateTime.UtcNow;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool Leida { get; set; }
    public bool Enviada { get; set; } = true;
    public string? ClaveUnica { get; set; }
}
