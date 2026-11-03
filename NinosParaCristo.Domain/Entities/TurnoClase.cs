namespace NinosParaCristo.Domain.Entities;

public class TurnoClase
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int MaestraId { get; set; }
    public Usuario Maestra { get; set; } = null!;
    public string? Tema { get; set; }
    public string? MaterialUrl { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
}
