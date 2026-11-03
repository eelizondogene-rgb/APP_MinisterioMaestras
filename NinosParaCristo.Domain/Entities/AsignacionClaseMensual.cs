namespace NinosParaCristo.Domain.Entities;

public class AsignacionClaseMensual
{
    public int Id { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public int GrupoClaseId { get; set; }
    public GrupoClase GrupoClase { get; set; } = null!;
    public int MaestraId { get; set; }
    public Usuario Maestra { get; set; } = null!;
    public string? Tema { get; set; }
    public string? MaterialUrl { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
}
