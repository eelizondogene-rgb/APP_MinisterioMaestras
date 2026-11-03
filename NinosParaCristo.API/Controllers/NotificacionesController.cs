using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public NotificacionesController(ApplicationDbContext context) => _context=context;

    [HttpGet("mias")]
    public async Task<ActionResult> Mias()
    {
        var uid=User.UserId();
        return Ok(await _context.Notificaciones.AsNoTracking().Where(x=>x.UsuarioId==uid)
            .OrderByDescending(x=>x.FechaCreacion)
            .Select(x=>new {x.Id,x.Titulo,x.Mensaje,x.Tipo,x.FechaProgramada,x.FechaCreacion,x.Leida}).Take(100).ToListAsync());
    }

    [HttpPut("{id:int}/leer")]
    public async Task<ActionResult> Leer(int id)
    {
        var uid=User.UserId();
        var n=await _context.Notificaciones.FirstOrDefaultAsync(x=>x.Id==id && x.UsuarioId==uid);
        if(n is null) return NotFound();
        n.Leida=true; await _context.SaveChangesAsync(); return NoContent();
    }
}
