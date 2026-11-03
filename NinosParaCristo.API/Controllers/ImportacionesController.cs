using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;
[ApiController,Route("api/importaciones"),Authorize(Roles=Roles.Administradora)]
public class ImportacionesController(ApplicationDbContext db,IPasswordHasher hasher):ControllerBase
{
    [HttpGet("{tipo}/plantilla")]
    public async Task<ActionResult> Template(string tipo)
    {
        if(!ExcelImportService.Columns.ContainsKey(tipo))return BadRequest();
        Response.Headers.CacheControl="no-store";
        using var book=new ClosedXML.Excel.XLWorkbook(new MemoryStream(ExcelImportService.Template(tipo)));
        var reference=book.AddWorksheet("Referencias");
        reference.Cell(1,1).Value="Maestras existentes (no se importan desde esta hoja)";
        reference.Cell(2,1).Value="Nombre";reference.Cell(2,2).Value="Correo";reference.Cell(2,3).Value="Funcion";
        var users=await db.Usuarios.AsNoTracking().Where(u=>u.Activo).OrderBy(u=>u.NombreCompleto).ToListAsync();
        for(int i=0;i<users.Count;i++){reference.Cell(i+3,1).Value=users[i].NombreCompleto;reference.Cell(i+3,2).Value=users[i].Email;reference.Cell(i+3,3).Value=users[i].Rol;}
        reference.Cell(1,5).Value="Grupos activos";
        var groups=await db.GruposClase.AsNoTracking().Where(g=>g.Activo).OrderBy(g=>g.Orden).ToListAsync();
        for(int i=0;i<groups.Count;i++)reference.Cell(i+2,5).Value=groups[i].Nombre;
        reference.Columns(1,5).Width=32;
        using var output=new MemoryStream();book.SaveAs(output);
        return File(output.ToArray(),"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"Plantilla_{tipo}.xlsx");
    }
    [HttpPost("{tipo}/excel"),RequestSizeLimit(2_500_000)]
    public async Task<ActionResult> Excel(string tipo,IFormFile archivo,[FromQuery]bool confirmar=false)
    {
        if(archivo==null||archivo.Length is <1 or >2_000_000||!Path.GetExtension(archivo.FileName).Equals(".xlsx",StringComparison.OrdinalIgnoreCase))return BadRequest(new{mensaje="Seleccione un archivo .xlsx de hasta 2 MB."});
        List<ImportRow> rows;
        try{using var stream=new MemoryStream();await archivo.CopyToAsync(stream);stream.Position=0;rows=ExcelImportService.Read(stream,tipo);}
        catch(Exception ex) when(ex is not OutOfMemoryException){return BadRequest(new{mensaje=ex is InvalidDataException?ex.Message:"No se pudo leer el Excel. Use la plantilla .xlsx, sin contraseña ni fórmulas."});}
        return await Process(tipo,rows,confirmar);
    }
    [HttpPost("{tipo}/lote"),RequestSizeLimit(2_500_000)]
    public Task<ActionResult> Batch(string tipo,List<ImportRow> rows,[FromQuery]bool confirmar=false)=>Process(tipo,rows,confirmar);
    private async Task<ActionResult> Process(string tipo,List<ImportRow> rows,bool confirmar)
    {
        Response.Headers.CacheControl="no-store";
        try{return Ok(await new ExcelImportService(db,hasher).Process(tipo,rows,confirmar));}
        catch(Exception ex) when(ex is DbUpdateException or Npgsql.PostgresException){return Conflict(new{mensaje="Hubo un cambio simultáneo o un conflicto. No se guardó la carga. Vuelva a revisar la vista previa."});}
    }
}
