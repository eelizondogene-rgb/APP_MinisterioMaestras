using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Asistencia;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/asistencias")]
[Authorize(Roles = Roles.Administradora + "," + Roles.MaestraClase)]
public class AsistenciasController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<UpdatesHub> _hub;

    public AsistenciasController(ApplicationDbContext context, IHubContext<UpdatesHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] DateOnly? fecha)
    {
        var f = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        var ninos = await NinosPermitidosAsync(f);
        var ids = ninos.Select(x => x.Id).ToHashSet();
        var asist = await _context.Asistencias.AsNoTracking()
            .Where(x => x.Fecha == f && ids.Contains(x.NinoId))
            .ToDictionaryAsync(x => x.NinoId);

        return Ok(ninos.Select(n => new
        {
            n.Id,
            n.NombreCompleto,
            n.Alergias,
            n.Grupo,
            Fecha = f,
            Presente = asist.TryGetValue(n.Id, out var a) ? a.Presente : (bool?)null,
            Observacion = asist.TryGetValue(n.Id, out a) ? a.Observacion : null
        }));
    }

    [HttpPost]
    public async Task<ActionResult> Save(GuardarAsistenciaDto dto)
    {
        var uid = User.UserId();

        if (!User.IsInRole(Roles.Administradora))
        {
            var permitidos = (await NinosPermitidosAsync(dto.Fecha)).Select(x => x.Id).ToHashSet();
            if (dto.Registros.Any(x => !permitidos.Contains(x.NinoId)))
                return Forbid();
        }

        foreach (var r in dto.Registros)
        {
            var a = await _context.Asistencias.FirstOrDefaultAsync(x => x.NinoId == r.NinoId && x.Fecha == dto.Fecha);
            if (a is null)
            {
                a = new Asistencia { NinoId = r.NinoId, Fecha = dto.Fecha };
                _context.Asistencias.Add(a);
            }

            a.Presente = r.Presente;
            a.Observacion = r.Observacion?.Trim();
            a.RegistradaPorUsuarioId = uid;
            a.FechaRegistro = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _hub.Clients.All.SendAsync("actualizacion", "asistencias");
        return NoContent();
    }

    private async Task<List<NinoAula>> NinosPermitidosAsync(DateOnly fecha)
    {
        var ninos = await _context.Ninos.AsNoTracking().Where(x => x.Activo)
            .OrderBy(x => x.NombreCompleto)
            .Select(x => new { x.Id, x.NombreCompleto, x.FechaNacimiento, x.Alergias })
            .ToListAsync();

        if (User.IsInRole(Roles.Administradora))
            return ninos.Select(n => new NinoAula(n.Id, n.NombreCompleto, n.Alergias, "Todos")).ToList();

        var uid = User.UserId();
        var grupos = await _context.AsignacionesClaseMensuales.AsNoTracking()
            .Include(x => x.GrupoClase)
            .Where(x => x.Activo && x.MaestraId == uid && x.Anio == fecha.Year && x.Mes == fecha.Month && x.GrupoClase.Activo)
            .Select(x => new { x.GrupoClase.Nombre, x.GrupoClase.EdadMinima, x.GrupoClase.EdadMaxima })
            .ToListAsync();

        var resultado = new List<NinoAula>();
        foreach (var n in ninos)
        {
            var edad = EdadEn(n.FechaNacimiento, fecha);
            var grupo = grupos.FirstOrDefault(g => edad >= g.EdadMinima && edad <= g.EdadMaxima);
            if (grupo is not null)
                resultado.Add(new NinoAula(n.Id, n.NombreCompleto, n.Alergias, grupo.Nombre));
        }
        return resultado;
    }

    private static int EdadEn(DateOnly nacimiento, DateOnly fecha)
    {
        var edad = fecha.Year - nacimiento.Year;
        if (nacimiento.AddYears(edad) > fecha) edad--;
        return edad;
    }

    private sealed record NinoAula(int Id, string NombreCompleto, string? Alergias, string Grupo);
}
