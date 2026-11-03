using System.ComponentModel.DataAnnotations;

namespace NinosParaCristo.Application.DTOs.Usuarios;

public class CrearUsuarioDto
{
    [Required, StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = string.Empty;
}

public class ActualizarUsuarioDto
{
    [Required, StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Rol { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;
}

public class CambiarPasswordDto
{
    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
