using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Controllers;
using NinosParaCristo.Application.DTOs.Calendarios;
using NinosParaCristo.Application.DTOs.Protocolo;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;
using NinosParaCristo.Infrastructure.Services;

static class OperacionTests
{
    public static async Task<int> Run(ApplicationDbContext db)
    {
        var count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("OK: "+name);count++;}
        DateTimeOffset At(string s)=>DateTimeOffset.Parse(s,System.Globalization.CultureInfo.InvariantCulture);
        Check(ReminderService.TargetSunday(At("2026-11-09T14:59:59Z")) is null,"Lunes antes de las 9 no genera avisos");
        Check(ReminderService.TargetSunday(At("2026-11-09T15:00:00Z"))==new DateOnly(2026,11,15),"Lunes 9 de Costa Rica selecciona el domingo siguiente");
        Check(ReminderService.TargetSunday(At("2026-11-10T00:00:00Z"))==new DateOnly(2026,11,15),"La fecha UTC no cambia la semana local");
        Check(ReminderService.TargetSunday(At("2026-11-15T06:00:00Z")) is null,"No envía avisos semanales vencidos el domingo");
        Check(ReminderService.TargetSunday(At("2026-12-28T15:00:00Z"))==new DateOnly(2027,1,3),"Recordatorio cruza correctamente año y mes");
        db.Usuarios.AddRange(
            new Usuario{Id=2,NombreCompleto="Clase dos",Email="clase2@example.invalid",PasswordHash="test",Rol="MaestraClase",Activo=true},
            new Usuario{Id=3,NombreCompleto="Protocolo uno",Email="protocolo1@example.invalid",PasswordHash="test",Rol="MaestraProtocolo",Activo=true},
            new Usuario{Id=4,NombreCompleto="Protocolo dos",Email="protocolo2@example.invalid",PasswordHash="test",Rol="MaestraProtocolo",Activo=true});
        db.GruposClase.Add(new GrupoClase{Id=1,Nombre="Grupo prueba",EdadMinima=1,EdadMaxima=5,Activo=true});
        await db.SaveChangesAsync();
        var cal=new CalendariosController(db,new EmptyHub());
        GuardarAsignacionClaseMensualDto Assignment(int month,int teacher=1)=>new(){Anio=2026,Mes=month,GrupoClaseId=1,MaestraId=teacher};
        Check(await cal.CrearAsignacion(Assignment(11)) is OkObjectResult,"Crear asignación mensual");
        var assignment=await db.AsignacionesClaseMensuales.SingleAsync();
        await cal.EliminarAsignacion(assignment.Id);
        Check(await cal.CrearAsignacion(Assignment(11)) is OkObjectResult && await db.AsignacionesClaseMensuales.CountAsync()==1 && assignment.Activo,"Quitar y volver a asignar reutiliza registro sin duplicar índice");
        Check(await cal.CrearAsignacion(Assignment(11)) is ConflictObjectResult,"Repetir asignación activa devuelve conflicto");
        Check(await cal.CargaTrimestral([Assignment(12),Assignment(12)]) is OkObjectResult && await db.AsignacionesClaseMensuales.CountAsync()==2,"Carga repetida no duplica filas");
        var before=await db.AsignacionesClaseMensuales.CountAsync();
        Check(await cal.CargaTrimestral([Assignment(1),Assignment(2,999)]) is BadRequestObjectResult && await db.AsignacionesClaseMensuales.CountAsync()==before,"Carga inválida no guarda meses parcialmente");
        var annual=Enumerable.Range(1,12).Select(m=>new GuardarAsignacionClaseMensualDto{Anio=2027,Mes=m,GrupoClaseId=1,MaestraId=2}).ToList();
        Check(await cal.CargaTrimestral(annual) is OkObjectResult && await db.AsignacionesClaseMensuales.CountAsync(x=>x.Anio==2027)==12,"Carga anual conserva doce asignaciones mensuales");
        var pro=new ProtocoloController(db,new EmptyHub());
        var sunday=new DateOnly(2026,11,15);
        GuardarProtocoloDto Protocol(int snack=3,int responsible=3)=>new(){Fecha=sunday,ResponsableMeriendaId=snack,ResponsableProtocoloId=responsible,Merienda="Fruta",Tareas="Recepción"};
        Check(await pro.Create(Protocol(1,3)) is BadRequestObjectResult,"Maestra de clase no puede asignarse a merienda");
        Check(await pro.Create(Protocol()) is OkObjectResult,"Una persona puede hacer merienda y protocolo");
        var protocol=await db.Protocolos.SingleAsync();
        await pro.Delete(protocol.Id);
        Check(await pro.Create(Protocol()) is OkObjectResult && await db.Protocolos.CountAsync()==1 && protocol.Activo,"Reactivar fecha de protocolo no duplica índice");
        var clock=new FixedClock(At("2026-11-09T14:59:59Z"));
        var reminders=new ReminderService(db,clock);
        Check(await reminders.GenerateAsync()==0,"Servicio no genera antes del horario");
        clock.Now=At("2026-11-09T15:00:00Z");
        Check(await reminders.GenerateAsync()==3,"Genera clase, merienda y protocolo el lunes");
        Check(await reminders.GenerateAsync()==0 && await db.Notificaciones.CountAsync()==3,"Repetición no duplica recordatorios");
        var classNotice=await db.Notificaciones.SingleAsync(x=>x.Tipo=="TurnoClase");
        classNotice.Enviada=true;await db.SaveChangesAsync();
        assignment.MaterialUrl="https://example.invalid/nuevo-material";await db.SaveChangesAsync();
        Check(await reminders.GenerateAsync()==1 && !classNotice.Enviada,"Cambio de material actualiza el aviso y permite reenviarlo");
        Check(await reminders.GenerateAsync()==0,"Material sin nuevos cambios no vuelve a encolar");
        await pro.Update(protocol.Id,Protocol(4,4));
        await reminders.GenerateAsync();
        Check(!await db.Notificaciones.AnyAsync(x=>x.UsuarioId==3 && !x.Enviada) && await db.Notificaciones.CountAsync(x=>x.UsuarioId==4)==2,"Cambio de responsable retira pendientes de la persona anterior");
        clock.Now=At("2026-11-15T06:00:00Z");
        await reminders.GenerateAsync();
        Check(!await db.Notificaciones.AnyAsync(x=>!x.Enviada),"Se retiran avisos semanales pendientes al vencer");
        clock.Now=At("2026-11-10T16:00:00Z");
        Check(await reminders.GenerateAsync()==3,"Reinicio posterior al lunes recupera avisos de la semana");
        var inactive=await db.Usuarios.FindAsync(4);inactive!.Activo=false;await db.SaveChangesAsync();
        await reminders.GenerateAsync();
        Check(!await db.Notificaciones.AnyAsync(x=>x.UsuarioId==4 && !x.Enviada),"Cuenta inactiva no conserva recordatorios pendientes");
        return count;
    }
}
class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now {get;set;}=now;
    public override DateTimeOffset GetUtcNow()=>Now;
}
