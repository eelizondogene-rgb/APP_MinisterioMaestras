using System.ComponentModel.DataAnnotations;
namespace NinosParaCristo.Application.DTOs.Inventario;

public class GuardarInventarioDto
{
    [Required, StringLength(150)] public string Nombre { get; set; } = string.Empty;
    [StringLength(500)] public string? Descripcion { get; set; }
    [Range(0, int.MaxValue)] public int Cantidad { get; set; }
    [Range(0, int.MaxValue)] public int StockMinimo { get; set; }
    [Required, StringLength(50)] public string Unidad { get; set; } = "unidad";
}
public class MovimientoInventarioDto
{
    [Required] public string Tipo { get; set; } = "Entrada";
    [Range(1, int.MaxValue)] public int Cantidad { get; set; }
    [StringLength(500)] public string? Nota { get; set; }
}
