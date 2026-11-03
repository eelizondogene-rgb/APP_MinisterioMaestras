namespace NinosParaCristo.Domain.Entities;

public class GrupoClase
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int EdadMinima { get; set; }
    public int EdadMaxima { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<AsignacionClaseMensual> Asignaciones { get; set; } = new List<AsignacionClaseMensual>();
}
