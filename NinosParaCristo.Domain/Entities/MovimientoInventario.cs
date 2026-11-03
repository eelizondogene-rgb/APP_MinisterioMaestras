namespace NinosParaCristo.Domain.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public int InventarioItemId { get; set; }
    public InventarioItem InventarioItem { get; set; } = null!;
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public string? Nota { get; set; }
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}
