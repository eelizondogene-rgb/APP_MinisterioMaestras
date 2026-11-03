using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Ninos;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/ninos")]
[Authorize]
public class NinosController : ControllerBase
{
    private readonly INinoService _service;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<UpdatesHub> _hub;

    public NinosController(INinoService service, ApplicationDbContext context, IHubContext<UpdatesHub> hub)
    {
        _service = service;
        _context = context;
        _hub = hub;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> GetAdmin() => Ok(await _service.ObtenerTodosAdminAsync());

    [HttpGet("aula")]
    [Authorize(Roles = Roles.Administradora + "," + Roles.MaestraClase)]
    public async Task<ActionResult> Aula([FromQuery] DateOnly? fecha)
    {
        if (User.IsInRole(Roles.Administradora))
            return Ok(await _service.ObtenerParaAulaAsync());

        var f = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        var uid = User.UserId();
        var grupos = await _context.AsignacionesClaseMensuales.AsNoTracking()
            .Include(x => x.GrupoClase)
            .Where(x => x.Activo && x.MaestraId == uid && x.Anio == f.Year && x.Mes == f.Month && x.GrupoClase.Activo)
            .Select(x => new { x.GrupoClase.Nombre, x.GrupoClase.EdadMinima, x.GrupoClase.EdadMaxima })
            .ToListAsync();

        if (grupos.Count == 0) return Ok(Array.Empty<object>());

        var ninos = await _context.Ninos.AsNoTracking().Where(x => x.Activo)
            .OrderBy(x => x.NombreCompleto)
            .Select(x => new { x.Id, x.NombreCompleto, x.FechaNacimiento, x.Alergias })
            .ToListAsync();

        var resultado = ninos.Select(n =>
        {
            var edad = EdadEn(n.FechaNacimiento, f);
            var grupo = grupos.FirstOrDefault(g => edad >= g.EdadMinima && edad <= g.EdadMaxima);
            return grupo is null ? null : new { n.Id, n.NombreCompleto, n.Alergias, Grupo = grupo.Nombre, Edad = edad };
        }).Where(x => x is not null);

        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Get(int id)
    {
        var n = await _service.ObtenerPorIdAsync(id);
        return n is null ? NotFound() : Ok(n);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Create(CrearNinoDto dto)
    {
        var n = await _service.CrearAsync(dto);
        await _hub.Clients.All.SendAsync("actualizacion", "ninos");
        return CreatedAtAction(nameof(Get), new { id = n.Id }, n);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Update(int id, ActualizarNinoDto dto)
    {
        var n = await _service.ActualizarAsync(id, dto);
        if (n is null) return NotFound();
        await _hub.Clients.All.SendAsync("actualizacion", "ninos");
        return Ok(n);
    }

    [HttpPut("{id:int}/encargado")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Encargado(int id, GuardarEncargadoDto dto)
    {
        var e = await _service.GuardarEncargadoPrincipalAsync(id, dto);
        if (e is null) return NotFound();
        await _hub.Clients.All.SendAsync("actualizacion", "ninos");
        return Ok(e);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Administradora)]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await _service.DesactivarAsync(id)) return NotFound();
        await _hub.Clients.All.SendAsync("actualizacion", "ninos");
        return NoContent();
    }

    private static int EdadEn(DateOnly nacimiento, DateOnly fecha)
    {
        var edad = fecha.Year - nacimiento.Year;
        if (nacimiento.AddYears(edad) > fecha) edad--;
        return edad;
    }
}
