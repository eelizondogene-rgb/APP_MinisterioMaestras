namespace NinosParaCristo.Domain.Entities;

public class InventarioItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Cantidad { get; set; }
    public int StockMinimo { get; set; }
    public string Unidad { get; set; } = "unidad";
    public bool Activo { get; set; } = true;
}
