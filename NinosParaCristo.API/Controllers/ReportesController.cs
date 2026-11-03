using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = Roles.Administradora)]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ReportesController(ApplicationDbContext context) => _context=context;

    [HttpGet("resumen")]
    public async Task<ActionResult> Resumen()
    {
        var total=await _context.Ninos.CountAsync(x=>x.Activo);
        var conAlergia=await _context.Ninos.CountAsync(x=>x.Activo && x.Alergias!=null && x.Alergias!="");
        var reponer=await _context.InventarioItems.CountAsync(x=>x.Activo && x.Cantidad<=x.StockMinimo);
        var desde=DateOnly.FromDateTime(DateTime.Now.AddDays(-90));
        var totalReg=await _context.Asistencias.CountAsync(x=>x.Fecha>=desde);
        var presentes=await _context.Asistencias.CountAsync(x=>x.Fecha>=desde && x.Presente);
        var promedio=totalReg==0?0:Math.Round((double)presentes/totalReg*100,1);
        return Ok(new { totalNinos=total, ninosConAlergias=conAlergia, inventarioPorReponer=reponer, asistenciaPromedioUltimos90Dias=promedio });
    }

    [HttpGet("cumpleanos")]
    public async Task<ActionResult> Cumpleanos([FromQuery] int? mes)
    {
        var m=mes ?? DateTime.Now.Month;
        return Ok(await _context.Ninos.AsNoTracking().Where(x=>x.Activo && x.FechaNacimiento.Month==m)
            .OrderBy(x=>x.FechaNacimiento.Day).Select(x=>new {x.Id,x.NombreCompleto,x.FechaNacimiento}).ToListAsync());
    }

    [HttpGet("alergias")]
    public async Task<ActionResult> Alergias() =>
        Ok(await _context.Ninos.AsNoTracking().Where(x=>x.Activo && x.Alergias!=null && x.Alergias!="")
            .OrderBy(x=>x.NombreCompleto).Select(x=>new {x.Id,x.NombreCompleto,x.Alergias}).ToListAsync());

    [HttpGet("asistencia-promedio")]
    public async Task<ActionResult> Asistencia([FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta)
    {
        var d=desde ?? DateOnly.FromDateTime(DateTime.Now.AddMonths(-3));
        var h=hasta ?? DateOnly.FromDateTime(DateTime.Now);
        var data=await _context.Asistencias.AsNoTracking().Where(x=>x.Fecha>=d && x.Fecha<=h)
            .GroupBy(x=>x.NinoId).Select(g=>new {NinoId=g.Key, Total=g.Count(), Presentes=g.Count(x=>x.Presente)}).ToListAsync();
        var names=await _context.Ninos.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.NombreCompleto);
        return Ok(data.Select(x=>new {x.NinoId, Nombre=names.GetValueOrDefault(x.NinoId), x.Total, x.Presentes, Porcentaje=x.Total==0?0:Math.Round((double)x.Presentes/x.Total*100,1)}));
    }

    [HttpGet("ausencias")]
    public async Task<ActionResult> Ausencias([FromQuery] int semanas=3)
    {
        semanas=Math.Clamp(semanas,1,12);
        var domingos=new List<DateOnly>();
        var d=DateTime.Today;
        while(d.DayOfWeek!=DayOfWeek.Sunday) d=d.AddDays(-1);
        for(var i=0;i<semanas;i++) domingos.Add(DateOnly.FromDateTime(d.AddDays(-7*i)));

        var ids=await _context.Ninos.Where(x=>x.Activo).Select(x=>new {x.Id,x.NombreCompleto}).ToListAsync();
        var asist=await _context.Asistencias.AsNoTracking().Where(x=>domingos.Contains(x.Fecha)).ToListAsync();
        var result=ids.Where(n=>domingos.All(fecha=>asist.Any(a=>a.NinoId==n.Id && a.Fecha==fecha && !a.Presente)))
            .Select(n=>new {n.Id,n.NombreCompleto,SemanasAusente=semanas,Desde=domingos.Min(),Hasta=domingos.Max()});
        return Ok(result);
    }
}
