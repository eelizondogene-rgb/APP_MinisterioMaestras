using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Calendarios;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/calendarios")]
[Authorize(Roles = Roles.Administradora + "," + Roles.MaestraClase)]
public class CalendariosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<UpdatesHub> _hub;

    public CalendariosController(ApplicationDbContext context, IHubContext<UpdatesHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet("grupos")]
    public async Task<ActionResult> Grupos([FromQuery] bool incluirInactivos = false)
    {
        var q = _context.GruposClase.AsNoTracking();
        if (!incluirInactivos || !User.IsInRole(Roles.Administradora))
            q = q.Where(x => x.Activo);

        return Ok(await q.OrderBy(x => x.Orden).ThenBy(x => x.EdadMinima).Select(x => new
        {
            x.Id,
            x.Nombre,
            x.EdadMinima,
            x.EdadMaxima,
            x.Orden,
            x.Activo
        }).ToListAsync());
    }

    [HttpPost("grupos")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> CrearGrupo(GuardarGrupoClaseDto dto)
    {
        if (dto.EdadMaxima < dto.EdadMinima)
            return BadRequest(new { mensaje = "La edad máxima no puede ser menor que la edad mínima." });

        var nombre = dto.Nombre.Trim();
        if (await _context.GruposClase.AnyAsync(x => x.Nombre.ToLower() == nombre.ToLower()))
            return Conflict(new { mensaje = "Ya existe un grupo con ese nombre." });

        var grupo = new GrupoClase
        {
            Nombre = nombre,
            EdadMinima = dto.EdadMinima,
            EdadMaxima = dto.EdadMaxima,
            Orden = dto.Orden,
            Activo = true
        };

        _context.GruposClase.Add(grupo);
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return Ok(grupo);
    }

    [HttpPut("grupos/{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> ActualizarGrupo(int id, GuardarGrupoClaseDto dto)
    {
        if (dto.EdadMaxima < dto.EdadMinima)
            return BadRequest(new { mensaje = "La edad máxima no puede ser menor que la edad mínima." });

        var grupo = await _context.GruposClase.FindAsync(id);
        if (grupo is null) return NotFound();

        grupo.Nombre = dto.Nombre.Trim();
        grupo.EdadMinima = dto.EdadMinima;
        grupo.EdadMaxima = dto.EdadMaxima;
        grupo.Orden = dto.Orden;

        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return NoContent();
    }

    [HttpDelete("grupos/{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> DesactivarGrupo(int id)
    {
        var grupo = await _context.GruposClase.FindAsync(id);
        if (grupo is null) return NotFound();
        grupo.Activo = false;
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return NoContent();
    }

    [HttpGet("clases")]
    public async Task<ActionResult> Clases([FromQuery] int? anio, [FromQuery] int? mes)
    {
        var year = anio ?? DateTime.Now.Year;
        var q = _context.AsignacionesClaseMensuales.AsNoTracking()
            .Include(x => x.GrupoClase)
            .Include(x => x.Maestra)
            .Where(x => x.Activo && x.Anio == year && x.GrupoClase.Activo && x.Maestra.Activo);

        if (mes.HasValue)
            q = q.Where(x => x.Mes == mes.Value);

        if (!User.IsInRole(Roles.Administradora))
        {
            var uid = User.UserId();
            q = q.Where(x => x.MaestraId == uid);
        }

        return Ok(await q
            .OrderBy(x => x.Mes)
            .ThenBy(x => x.GrupoClase.Orden)
            .ThenBy(x => x.Maestra.NombreCompleto)
            .Select(x => new
            {
                x.Id,
                x.Anio,
                x.Mes,
                x.GrupoClaseId,
                Grupo = x.GrupoClase.Nombre,
                x.GrupoClase.EdadMinima,
                x.GrupoClase.EdadMaxima,
                x.MaestraId,
                Maestra = x.Maestra.NombreCompleto,
                x.Tema,
                x.MaterialUrl,
                x.Observaciones
            })
            .ToListAsync());
    }

    [HttpPost("clases")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> CrearAsignacion(GuardarAsignacionClaseMensualDto dto)
    {
        var error = await ValidarAsignacion(dto);
        if (error is not null) return BadRequest(new { mensaje = error });

        var existe = await _context.AsignacionesClaseMensuales.AnyAsync(x =>
            x.Activo && x.Anio == dto.Anio && x.Mes == dto.Mes &&
            x.GrupoClaseId == dto.GrupoClaseId && x.MaestraId == dto.MaestraId);
        if (existe) return Conflict(new { mensaje = "Esa maestra ya está asignada a ese grupo en el mes seleccionado." });

        var item = CrearEntidad(dto);
        _context.AsignacionesClaseMensuales.Add(item);
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return Ok(item);
    }

    [HttpPut("clases/{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> ActualizarAsignacion(int id, GuardarAsignacionClaseMensualDto dto)
    {
        var error = await ValidarAsignacion(dto);
        if (error is not null) return BadRequest(new { mensaje = error });

        var item = await _context.AsignacionesClaseMensuales.FindAsync(id);
        if (item is null) return NotFound();

        var duplicada = await _context.AsignacionesClaseMensuales.AnyAsync(x => x.Id != id && x.Activo &&
            x.Anio == dto.Anio && x.Mes == dto.Mes && x.GrupoClaseId == dto.GrupoClaseId && x.MaestraId == dto.MaestraId);
        if (duplicada) return Conflict(new { mensaje = "Ya existe esa asignación." });

        item.Anio = dto.Anio;
        item.Mes = dto.Mes;
        item.GrupoClaseId = dto.GrupoClaseId;
        item.MaestraId = dto.MaestraId;
        item.Tema = dto.Tema?.Trim();
        item.MaterialUrl = dto.MaterialUrl?.Trim();
        item.Observaciones = dto.Observaciones?.Trim();

        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return NoContent();
    }

    [HttpDelete("clases/{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> EliminarAsignacion(int id)
    {
        var item = await _context.AsignacionesClaseMensuales.FindAsync(id);
        if (item is null) return NotFound();
        item.Activo = false;
        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        return NoContent();
    }

    [HttpPost("clases/carga-trimestral")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> CargaTrimestral(List<GuardarAsignacionClaseMensualDto> asignaciones)
    {
        var agregadas = 0;
        var omitidas = 0;

        foreach (var dto in asignaciones)
        {
            var error = await ValidarAsignacion(dto);
            if (error is not null) { omitidas++; continue; }

            var existe = await _context.AsignacionesClaseMensuales.AnyAsync(x =>
                x.Activo && x.Anio == dto.Anio && x.Mes == dto.Mes &&
                x.GrupoClaseId == dto.GrupoClaseId && x.MaestraId == dto.MaestraId);
            if (existe) { omitidas++; continue; }

            _context.AsignacionesClaseMensuales.Add(CrearEntidad(dto));
            agregadas++;
        }

        if (agregadas > 0)
        {
            await _context.SaveChangesAsync();
            await _hub.Clients.All.SendAsync("actualizacion", "calendarios");
        }

        return Ok(new { agregadas, omitidas });
    }

    private async Task<string?> ValidarAsignacion(GuardarAsignacionClaseMensualDto dto)
    {
        if (dto.Mes is < 1 or > 12) return "El mes no es válido.";
        if (!await _context.GruposClase.AnyAsync(x => x.Id == dto.GrupoClaseId && x.Activo))
            return "El grupo de clase no es válido.";
        if (!await _context.Usuarios.AnyAsync(x => x.Id == dto.MaestraId && x.Activo && x.Rol == Roles.MaestraClase))
            return "La maestra seleccionada no tiene el rol Maestra de clase.";
        return null;
    }

    private static AsignacionClaseMensual CrearEntidad(GuardarAsignacionClaseMensualDto dto) => new()
    {
        Anio = dto.Anio,
        Mes = dto.Mes,
        GrupoClaseId = dto.GrupoClaseId,
        MaestraId = dto.MaestraId,
        Tema = dto.Tema?.Trim(),
        MaterialUrl = dto.MaterialUrl?.Trim(),
        Observaciones = dto.Observaciones?.Trim(),
        Activo = true
    };
}
