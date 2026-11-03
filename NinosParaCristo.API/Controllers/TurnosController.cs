using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Turnos;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/turnos")]
[Authorize(Roles = Roles.Administradora + "," + Roles.MaestraClase + "," + Roles.MaestraRotativa)]
public class TurnosController : ControllerBase
{
    private readonly ApplicationDbContext _context; private readonly IHubContext<UpdatesHub> _hub;
    public TurnosController(ApplicationDbContext context, IHubContext<UpdatesHub> hub) { _context=context; _hub=hub; }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var d = desde ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
        var h = hasta ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(4));
        var q = _context.TurnosClase.AsNoTracking().Include(x => x.Maestra)
            .Where(x => x.Activo && x.Fecha >= d && x.Fecha <= h);

        if (!User.IsInRole(Roles.Administradora))
        {
            var uid = User.UserId();
            q = q.Where(x => x.MaestraId == uid);
        }

        return Ok(await q.OrderBy(x => x.Fecha).Select(x => new
        {
            x.Id, x.Fecha, x.MaestraId, Maestra=x.Maestra.NombreCompleto, x.Tema, x.MaterialUrl, x.Observaciones
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Create(GuardarTurnoClaseDto dto)
    {
        if (!await _context.Usuarios.AnyAsync(x => x.Id==dto.MaestraId && x.Activo))
            return BadRequest(new { mensaje="Maestra no válida." });

        var item = new TurnoClase { Fecha=dto.Fecha, MaestraId=dto.MaestraId, Tema=dto.Tema?.Trim(), MaterialUrl=dto.MaterialUrl?.Trim(), Observaciones=dto.Observaciones?.Trim(), Activo=true };
        _context.TurnosClase.Add(item); await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","turnos");
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Update(int id, GuardarTurnoClaseDto dto)
    {
        var item = await _context.TurnosClase.FindAsync(id); if (item is null) return NotFound();
        item.Fecha=dto.Fecha; item.MaestraId=dto.MaestraId; item.Tema=dto.Tema?.Trim(); item.MaterialUrl=dto.MaterialUrl?.Trim(); item.Observaciones=dto.Observaciones?.Trim();
        await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","turnos"); return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await _context.TurnosClase.FindAsync(id); if (item is null) return NotFound();
        item.Activo=false; await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","turnos"); return NoContent();
    }

    [HttpPost("carga-trimestral")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Batch(List<GuardarTurnoClaseDto> turnos)
    {
        foreach (var dto in turnos)
            _context.TurnosClase.Add(new TurnoClase { Fecha=dto.Fecha, MaestraId=dto.MaestraId, Tema=dto.Tema?.Trim(), MaterialUrl=dto.MaterialUrl?.Trim(), Observaciones=dto.Observaciones?.Trim(), Activo=true });
        await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","turnos"); return Ok(new { agregados=turnos.Count });
    }
}
