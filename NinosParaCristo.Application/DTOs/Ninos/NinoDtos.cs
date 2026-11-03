using System.ComponentModel.DataAnnotations;

namespace NinosParaCristo.Application.DTOs.Ninos;

public class CrearNinoDto
{
    [Required(ErrorMessage = "El nombre del niño es obligatorio.")]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    public DateOnly FechaNacimiento { get; set; }

    [StringLength(500)]
    public string? Alergias { get; set; }

    [StringLength(1000)]
    public string? Comentarios { get; set; }

    [StringLength(150)]
    public string? EncargadoNombre { get; set; }

    [StringLength(50)]
    public string? EncargadoTelefono { get; set; }

    [EmailAddress, StringLength(200)]
    public string? EncargadoEmail { get; set; }

    [StringLength(80)]
    public string? EncargadoParentesco { get; set; }
}

public class ActualizarNinoDto
{
    [Required, StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    public DateOnly FechaNacimiento { get; set; }

    [StringLength(500)]
    public string? Alergias { get; set; }

    [StringLength(1000)]
    public string? Comentarios { get; set; }

    public bool Activo { get; set; } = true;
}

public class NinoDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string? Alergias { get; set; }
    public string? Comentarios { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public EncargadoDto? EncargadoPrincipal { get; set; }
}

public class NinoAulaDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Alergias { get; set; }
}

public class EncargadoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Parentesco { get; set; }
}

public class GuardarEncargadoDto
{
    [Required, StringLength(150)]
    public string Nombre { get; set; } = string.Empty;
    [StringLength(50)]
    public string? Telefono { get; set; }
    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }
    [StringLength(80)]
    public string? Parentesco { get; set; }
}
