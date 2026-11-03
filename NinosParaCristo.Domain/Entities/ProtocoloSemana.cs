namespace NinosParaCristo.Domain.Entities;

public class ProtocoloSemana
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public int? ResponsableMeriendaId { get; set; }
    public Usuario? ResponsableMerienda { get; set; }
    public int? ResponsableProtocoloId { get; set; }
    public Usuario? ResponsableProtocolo { get; set; }
    public string? Merienda { get; set; }
    public string? Tareas { get; set; }
    public string? Bitacora { get; set; }
    public bool Completado { get; set; }
    public bool Activo { get; set; } = true;
}
