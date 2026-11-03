using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.DTOs.Auth;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly JwtTokenService _jwt;

    public AuthController(ApplicationDbContext context, IPasswordHasher hasher, JwtTokenService jwt)
    {
        _context = context; _hasher = hasher; _jwt = jwt;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == email && x.Activo);
        if (user is null || !_hasher.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });

        return Ok(new LoginResponseDto
        {
            Token = _jwt.Create(user),
            Usuario = new UsuarioSesionDto { Id = user.Id, NombreCompleto = user.NombreCompleto, Email = user.Email, Rol = user.Rol }
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UsuarioSesionDto>> Me()
    {
        var user = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(x => x.Id == User.UserId());
        if (user is null) return Unauthorized();
        return Ok(new UsuarioSesionDto { Id = user.Id, NombreCompleto = user.NombreCompleto, Email = user.Email, Rol = user.Rol });
    }
}
