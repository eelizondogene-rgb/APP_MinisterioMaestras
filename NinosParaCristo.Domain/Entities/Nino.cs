namespace NinosParaCristo.Domain.Entities;

public class Nino
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? Comentarios { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public ICollection<Encargado> Encargados { get; set; } = new List<Encargado>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}
