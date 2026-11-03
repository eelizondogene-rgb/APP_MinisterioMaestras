namespace NinosParaCristo.Domain.Entities;

public class Asistencia
{
    public int Id { get; set; }
    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;
    public DateOnly Fecha { get; set; }
    public bool Presente { get; set; }
    public string? Observacion { get; set; }
    public int? RegistradaPorUsuarioId { get; set; }
    public Usuario? RegistradaPorUsuario { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
