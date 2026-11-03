using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Usuarios;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = Roles.Administradora)]
public class UsuariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly IHubContext<UpdatesHub> _hub;

    public UsuariosController(ApplicationDbContext context, IPasswordHasher hasher, IHubContext<UpdatesHub> hub)
    { _context = context; _hasher = hasher; _hub = hub; }

    [HttpGet]
    public async Task<ActionResult> Get() =>
        Ok(await _context.Usuarios.AsNoTracking().OrderBy(x => x.NombreCompleto)
            .Select(x => new UsuarioDto { Id=x.Id, NombreCompleto=x.NombreCompleto, Email=x.Email, Rol=x.Rol, Activo=x.Activo }).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(CrearUsuarioDto dto)
    {
        if (!Roles.Todos.Contains(dto.Rol)) return BadRequest(new { mensaje="Rol inválido." });
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(x => x.Email == email)) return Conflict(new { mensaje="El correo ya existe." });
        var user = new Usuario { NombreCompleto=dto.NombreCompleto.Trim(), Email=email, Rol=dto.Rol, PasswordHash=_hasher.Hash(dto.Password), Activo=true };
        _context.Usuarios.Add(user); await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion","usuarios");
        return Ok(new UsuarioDto { Id=user.Id, NombreCompleto=user.NombreCompleto, Email=user.Email, Rol=user.Rol, Activo=user.Activo });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, ActualizarUsuarioDto dto)
    {
        if (!Roles.Todos.Contains(dto.Rol)) return BadRequest(new { mensaje="Rol inválido." });
        var user = await _context.Usuarios.FindAsync(id); if (user is null) return NotFound();
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _context.Usuarios.AnyAsync(x => x.Email == email && x.Id != id)) return Conflict(new { mensaje="El correo ya existe." });
        user.NombreCompleto=dto.NombreCompleto.Trim(); user.Email=email; user.Rol=dto.Rol; user.Activo=dto.Activo;
        await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","usuarios");
        return NoContent();
    }

    [HttpPut("{id:int}/password")]
    public async Task<ActionResult> Password(int id, CambiarPasswordDto dto)
    {
        var user = await _context.Usuarios.FindAsync(id); if (user is null) return NotFound();
        user.PasswordHash = _hasher.Hash(dto.Password); await _context.SaveChangesAsync(); return NoContent();
    }
}
