namespace NinosParaCristo.Domain.Entities;

public class Encargado
{
    public int Id { get; set; }
    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Parentesco { get; set; }
    public bool EsPrincipal { get; set; } = true;
}
