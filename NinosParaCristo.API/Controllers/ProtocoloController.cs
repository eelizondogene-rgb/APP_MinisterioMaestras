using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Protocolo;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/protocolo")]
[Authorize]
public class ProtocoloController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<UpdatesHub> _hub;

    public ProtocoloController(ApplicationDbContext context, IHubContext<UpdatesHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet("merienda")]
    [Authorize(Roles = Roles.Administradora + "," + Roles.MaestraClase + "," + Roles.MaestraProtocolo + "," + Roles.MaestraRotativa)]
    public async Task<ActionResult> Merienda([FromQuery] DateOnly fecha)
    {
        var item = await _context.Protocolos.AsNoTracking().Where(x => x.Activo && x.Fecha == fecha)
            .Select(x => new { x.Fecha, x.Merienda }).FirstOrDefaultAsync();
        return Ok(item ?? new { Fecha = fecha, Merienda = (string?)null });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administradora + "," + Roles.MaestraProtocolo + "," + Roles.MaestraRotativa)]
    public async Task<ActionResult> Get([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var d = desde ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));
        var h = hasta ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(4));
        var q = _context.Protocolos.AsNoTracking()
            .Include(x => x.ResponsableMerienda)
            .Include(x => x.ResponsableProtocolo)
            .Where(x => x.Activo && x.Fecha >= d && x.Fecha <= h);

        if (!User.IsInRole(Roles.Administradora))
        {
            var uid = User.UserId();
            q = q.Where(x => x.ResponsableMeriendaId == uid || x.ResponsableProtocoloId == uid);
        }

        return Ok(await q.OrderBy(x => x.Fecha).Select(x => new
        {
            x.Id,
            x.Fecha,
            x.ResponsableMeriendaId,
            ResponsableMerienda = x.ResponsableMerienda != null ? x.ResponsableMerienda.NombreCompleto : null,
            x.ResponsableProtocoloId,
            ResponsableProtocolo = x.ResponsableProtocolo != null ? x.ResponsableProtocolo.NombreCompleto : null,
            x.Merienda,
            x.Tareas,
            x.Bitacora,
            x.Completado
        }).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Create(GuardarProtocoloDto dto)
    {
        var error = await ValidarResponsables(dto);
        if (error is not null) return BadRequest(new { mensaje = error });
        var existing = await _context.Protocolos.FirstOrDefaultAsync(x => x.Fecha == dto.Fecha);
        if (existing?.Activo == true)
            return Conflict(new { mensaje = "Ya existe una asignación para esa fecha. Puedes editarla desde el calendario." });

        var item = existing ?? new ProtocoloSemana();
        item.Fecha = dto.Fecha;
        item.ResponsableMeriendaId = dto.ResponsableMeriendaId;
        item.ResponsableProtocoloId = dto.ResponsableProtocoloId;
        item.Merienda = dto.Merienda?.Trim();
        item.Tareas = dto.Tareas?.Trim();
        item.Activo = true;
        if (existing is null) _context.Protocolos.Add(item);
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "protocolo");
        return Ok(item);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Update(int id, GuardarProtocoloDto dto)
    {
        var error = await ValidarResponsables(dto);
        if (error is not null) return BadRequest(new { mensaje = error });
        var item = await _context.Protocolos.FindAsync(id);
        if (item is null) return NotFound();

        var duplicado = await _context.Protocolos.AnyAsync(x => x.Id != id && x.Fecha == dto.Fecha);
        if (duplicado) return Conflict(new { mensaje = "Ya existe otra asignación para esa fecha." });

        item.Fecha = dto.Fecha;
        item.ResponsableMeriendaId = dto.ResponsableMeriendaId;
        item.ResponsableProtocoloId = dto.ResponsableProtocoloId;
        item.Merienda = dto.Merienda?.Trim();
        item.Tareas = dto.Tareas?.Trim();
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "protocolo");
        return NoContent();
    }

    [HttpPost("generar-domingos")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> GenerarDomingos([FromQuery] int anio, [FromQuery] int trimestre)
    {
        if (anio is < 2020 or > 2100 || trimestre is < 1 or > 4)
            return BadRequest(new { mensaje = "Año o trimestre no válido." });

        var mesInicial = (trimestre - 1) * 3 + 1;
        var inicio = new DateOnly(anio, mesInicial, 1);
        var fin = inicio.AddMonths(3).AddDays(-1);
        var fecha = inicio;
        while (fecha.DayOfWeek != DayOfWeek.Sunday) fecha = fecha.AddDays(1);

        var existentes = await _context.Protocolos.Where(x => x.Fecha >= inicio && x.Fecha <= fin)
            .Select(x => x.Fecha).ToListAsync();
        var set = existentes.ToHashSet();
        var agregados = 0;

        while (fecha <= fin)
        {
            if (!set.Contains(fecha))
            {
                _context.Protocolos.Add(new ProtocoloSemana
                {
                    Fecha = fecha,
                    Tareas = "Limpieza de pasillos y baños, recepción de la ofrenda y entrega de la merienda",
                    Activo = true
                });
                agregados++;
            }
            fecha = fecha.AddDays(7);
        }

        if (agregados > 0)
        {
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("actualizacion", "protocolo");
        }

        return Ok(new { agregados });
    }

    [HttpPut("{id:int}/bitacora")]
    [Authorize(Roles = Roles.Administradora + "," + Roles.MaestraProtocolo + "," + Roles.MaestraRotativa)]
    public async Task<ActionResult> Bitacora(int id, ActualizarBitacoraDto dto)
    {
        var item = await _context.Protocolos.FindAsync(id);
        if (item is null) return NotFound();
        if (!User.IsInRole(Roles.Administradora) && item.ResponsableProtocoloId != User.UserId() && item.ResponsableMeriendaId != User.UserId())
            return Forbid();

        item.Bitacora = dto.Bitacora?.Trim();
        item.Completado = dto.Completado;
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "protocolo");
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await _context.Protocolos.FindAsync(id);
        if (item is null) return NotFound();
        item.Activo = false;
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "protocolo");
        return NoContent();
    }
    private async Task<string?> ValidarResponsables(GuardarProtocoloDto dto)
    {
        if (dto.Fecha == default || dto.Fecha.DayOfWeek != DayOfWeek.Sunday)
            return "Seleccione un domingo válido.";
        var ids = new[] { dto.ResponsableMeriendaId, dto.ResponsableProtocoloId }
            .Where(x=>x.HasValue).Select(x=>x!.Value).Distinct().ToArray();
        if (await _context.Usuarios.CountAsync(x=>ids.Contains(x.Id) && x.Activo && (x.Rol==Roles.MaestraProtocolo || x.Rol==Roles.MaestraRotativa || x.Rol==Roles.Administradora)) != ids.Length)
            return "Merienda y protocolo requieren responsables activos con rol Maestra de protocolo.";
        if (await _context.AsignacionesClaseMensuales.AnyAsync(x=>x.Activo && x.Anio==dto.Fecha.Year && x.Mes==dto.Fecha.Month && ids.Contains(x.MaestraId)))
            return "Una responsable tiene clases asignadas en ese mes. Revise los turnos antes de asignarle protocolo o merienda.";
        return null;
    }

}
