using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Application.Interfaces;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[NonController] // Carga inicial retirada; se conserva el código para pruebas históricas.
[Route("api/carga-inicial-2026")]
[Authorize(Roles=Roles.Administradora)]
public class CargaInicialController(ApplicationDbContext db, IPasswordHasher hasher) : ControllerBase
{
    [HttpGet]
    public ActionResult Preview() => Ok(new {
        maestras=CalendarioInicial2026.Maestras, clases=CalendarioInicial2026.Clases,
        protocolos=CalendarioInicial2026.Protocolos,
        nota="Solo noviembre y diciembre de 2026. No se asignan meriendas ni horas: no aparecen en la imagen."
    });

    [HttpPost]
    public async Task<ActionResult> Import(List<CuentaInicialDto> cuentas)
    {
        Response.Headers.CacheControl="no-store";
        if(cuentas is null || cuentas.Count!=CalendarioInicial2026.Maestras.Length ||
           cuentas.Any(x=>x is null || string.IsNullOrWhiteSpace(x.Nombre) || string.IsNullOrWhiteSpace(x.Email) ||
               x.Email.Length>200 || !new EmailAddressAttribute().IsValid(x.Email.Trim())) ||
           cuentas.Select(x=>x.Nombre).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=cuentas.Count ||
           cuentas.Select(x=>x.Email.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=cuentas.Count)
            return BadRequest(new {mensaje="Complete un correo válido y diferente para cada una de las 18 maestras."});
        if(CalendarioInicial2026.Maestras.Any(m=>!cuentas.Any(x=>x.Nombre==m.Nombre)))
            return BadRequest(new {mensaje="La lista debe coincidir con las maestras del calendario."});

        // PostgreSQL: toda la carga se confirma junta o se revierte si encuentra un conflicto.
        await using var tx = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
        var users=await db.Usuarios.ToListAsync();
        var groups=await db.GruposClase.ToListAsync();
        var expectedGroups=new[] {("Cuna",1,3),("Párvulos",4,5),("Primarios",6,8),("Intermedios y Adolescentes",9,14)};
        foreach(var (name,min,max) in expectedGroups)
            if(!groups.Any(x=>x.Nombre==name && x.Activo && x.EdadMinima==min && x.EdadMaxima==max))
                return Conflict(new {mensaje=$"Revise el grupo {name} y sus edades antes de importar."});
        var existingMap=new Dictionary<string,Usuario>();
        foreach(var entry in cuentas)
        {
            var email=entry.Email.Trim().ToLowerInvariant();
            var expected=CalendarioInicial2026.Maestras.Single(x=>x.Nombre==entry.Nombre);
            var existing=users.SingleOrDefault(x=>x.Email.Equals(email,StringComparison.OrdinalIgnoreCase));
            if(existing is not null)
            {
                if(!existing.Activo || (existing.Rol!=Roles.Administradora &&
                    (existing.Rol!=expected.Rol || !existing.NombreCompleto.Equals(entry.Nombre,StringComparison.OrdinalIgnoreCase))))
                    return Conflict(new {mensaje=$"El correo de {entry.Nombre} ya pertenece a una cuenta con nombre, estado o rol diferente. Revíselo en Usuarios."});
                existingMap[entry.Nombre]=existing;
            }
            else if(users.Any(x=>x.NombreCompleto.Equals(entry.Nombre,StringComparison.OrdinalIgnoreCase)))
                return Conflict(new {mensaje=$"Ya existe una cuenta llamada {entry.Nombre}. Use su correo para evitar duplicarla."});
        }
        var assignments=await db.AsignacionesClaseMensuales.Where(x=>x.Anio==2026 && (x.Mes==11 || x.Mes==12)).ToListAsync();
        var protocols=await db.Protocolos.Where(x=>x.Fecha.Year==2026 && (x.Fecha.Month==11 || x.Fecha.Month==12)).ToListAsync();
        foreach(var existing in assignments.Where(x=>x.Activo))
        {
            if(!CalendarioInicial2026.Clases.Any(x=>x.Mes==existing.Mes && groups.Single(g=>g.Id==existing.GrupoClaseId).Nombre==x.Grupo &&
                existingMap.TryGetValue(x.Maestra,out var u) && u.Id==existing.MaestraId))
                return Conflict(new {mensaje="Ya hay clases distintas en noviembre o diciembre. Revise el calendario antes de importar; no se sobrescribió nada."});
        }
        foreach(var row in protocols)
        {
            var source=CalendarioInicial2026.Protocolos.SingleOrDefault(x=>x.Fecha==row.Fecha);
            if(!row.Activo && source is null) continue;
            if(row.Activo && (source is null || (row.ResponsableProtocoloId.HasValue &&
                (!existingMap.TryGetValue(source.Responsable,out var u) || u.Id!=row.ResponsableProtocoloId))))
                return Conflict(new {mensaje="Ya hay responsables de protocolo diferentes. Revise esas fechas antes de importar; no se sobrescribió nada."});
            if(row.ResponsableMeriendaId is int snack && CalendarioInicial2026.Clases.Any(x=>x.Mes==row.Fecha.Month &&
                existingMap.TryGetValue(x.Maestra,out var u) && u.Id==snack))
                return Conflict(new {mensaje="Una responsable de merienda coincide con clases en ese mes. Revise esa asignación antes de importar."});
        }
        var credentials=new List<CredencialInicial>();
        foreach(var entry in cuentas)
        {
            if(existingMap.ContainsKey(entry.Nombre)) continue;
            var password=Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
            var user=new Usuario {NombreCompleto=entry.Nombre,Email=entry.Email.Trim().ToLowerInvariant(),
                Rol=CalendarioInicial2026.Maestras.Single(x=>x.Nombre==entry.Nombre).Rol,
                PasswordHash=hasher.Hash(password),Activo=true,FechaRegistro=DateTime.UtcNow};
            db.Usuarios.Add(user);existingMap[entry.Nombre]=user;
            credentials.Add(new CredencialInicial(entry.Nombre,user.Email,password));
        }
        try
        {
            await db.SaveChangesAsync();
            foreach(var source in CalendarioInicial2026.Clases)
            {
                var uid=existingMap[source.Maestra].Id;
                var gid=groups.Single(x=>x.Nombre==source.Grupo).Id;
                var row=assignments.SingleOrDefault(x=>x.Mes==source.Mes && x.MaestraId==uid && x.GrupoClaseId==gid);
                if(row is null) db.AsignacionesClaseMensuales.Add(new AsignacionClaseMensual {Anio=2026,Mes=source.Mes,MaestraId=uid,GrupoClaseId=gid,Activo=true});
                else row.Activo=true;
            }
            foreach(var source in CalendarioInicial2026.Protocolos)
            {
                var row=protocols.SingleOrDefault(x=>x.Fecha==source.Fecha);
                if(row is null){row=new ProtocoloSemana{Fecha=source.Fecha};db.Protocolos.Add(row);}
                row.ResponsableProtocoloId=existingMap[source.Responsable].Id;row.Activo=true;
            }
            await db.SaveChangesAsync();
            if(tx is not null) await tx.CommitAsync();
        }
        catch(DbUpdateException)
        {
            return Conflict(new {mensaje="La carga encontró un conflicto de datos. No se confirmó. Recargue y revise antes de reintentar."});
        }
        return Ok(new {mensaje="Cuentas y calendario cargados.",cuentasCreadas=credentials.Count,
            asignacionesClase=CalendarioInicial2026.Clases.Length,fechasProtocolo=CalendarioInicial2026.Protocolos.Length,
            credenciales=credentials});
    }
}
public record CuentaInicialDto(string Nombre,string Email);
public record CredencialInicial(string Nombre,string Email,string Password);
