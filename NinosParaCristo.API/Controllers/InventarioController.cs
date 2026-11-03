using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.DTOs.Inventario;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/inventario")]
[Authorize(Roles = Roles.Administradora)]
public class InventarioController : ControllerBase
{
    private readonly ApplicationDbContext _context; private readonly IHubContext<UpdatesHub> _hub;
    public InventarioController(ApplicationDbContext context, IHubContext<UpdatesHub> hub) { _context=context; _hub=hub; }

    [HttpGet]
    public async Task<ActionResult> Get() => Ok(await _context.InventarioItems.AsNoTracking().Where(x=>x.Activo).OrderBy(x=>x.Nombre)
        .Select(x=>new {x.Id,x.Nombre,x.Descripcion,x.Cantidad,x.StockMinimo,x.Unidad, Reponer=x.Cantidad<=x.StockMinimo}).ToListAsync());

    [HttpPost]
    public async Task<ActionResult> Create(GuardarInventarioDto dto)
    {
        var item=new InventarioItem { Nombre=dto.Nombre.Trim(), Descripcion=dto.Descripcion?.Trim(), Cantidad=dto.Cantidad, StockMinimo=dto.StockMinimo, Unidad=dto.Unidad.Trim(), Activo=true };
        _context.InventarioItems.Add(item); await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","inventario"); return Ok(item);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, GuardarInventarioDto dto)
    {
        var item=await _context.InventarioItems.FindAsync(id); if(item is null) return NotFound();
        item.Nombre=dto.Nombre.Trim(); item.Descripcion=dto.Descripcion?.Trim(); item.StockMinimo=dto.StockMinimo; item.Unidad=dto.Unidad.Trim();
        await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","inventario"); return NoContent();
    }

    [HttpPost("{id:int}/movimientos")]
    public async Task<ActionResult> Move(int id, MovimientoInventarioDto dto)
    {
        var item=await _context.InventarioItems.FindAsync(id); if(item is null) return NotFound();
        var tipo=dto.Tipo.Trim().ToLowerInvariant();
        if(tipo!="entrada" && tipo!="salida") return BadRequest(new { mensaje="Tipo debe ser Entrada o Salida." });
        var delta=tipo=="entrada"?dto.Cantidad:-dto.Cantidad;
        if(item.Cantidad+delta<0) return BadRequest(new { mensaje="No hay suficiente inventario." });
        item.Cantidad+=delta;
        _context.MovimientosInventario.Add(new MovimientoInventario { InventarioItemId=id, Tipo=tipo=="entrada"?"Entrada":"Salida", Cantidad=dto.Cantidad, Nota=dto.Nota?.Trim(), UsuarioId=User.UserId(), FechaRegistro=DateTime.UtcNow });
        await _context.SaveChangesAsync(); await _hub.Clients.All.SendAsync("actualizacion","inventario"); return Ok(new { item.Id,item.Cantidad });
    }
}
