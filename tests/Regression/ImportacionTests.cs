using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Controllers;
using NinosParaCristo.API.Services;
using NinosParaCristo.Application.DTOs.Calendarios;
using NinosParaCristo.Application.DTOs.Protocolo;
using NinosParaCristo.Infrastructure.Persistence;
using NinosParaCristo.Infrastructure.Services;

static class ImportacionTests
{
    public static async Task<int> Run()
    {
        var options=new DbContextOptionsBuilder<ApplicationDbContext>();
        var connection=Environment.GetEnvironmentVariable("NPC_TEST_POSTGRES");
        if(string.IsNullOrWhiteSpace(connection))options.UseInMemoryDatabase(Guid.NewGuid().ToString());
        else {
            var target=new Npgsql.NpgsqlConnectionStringBuilder(connection);
            if(target.Host!="127.0.0.1" || target.Database is null || !target.Database.StartsWith("npc_regression_"))throw new Exception("Destino de pruebas inválido");
            target.Database+="_import";options.UseNpgsql(target.ConnectionString);
        }
        await using var db=new ApplicationDbContext(options.Options);
        await db.Database.EnsureCreatedAsync();
        var hasher=new PasswordHasher();
        await SeedData.EnsureAdminAsync(db,hasher,"admin@example.invalid","SoloPruebaTemporal123!","Administradora");
        await SeedData.EnsureGruposClaseAsync(db);
        var count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("OK: "+name);count++;}
        CargaInicialController Controller()=>new(db,hasher){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()}};
        var roster=CalendarioInicial2026.Maestras.Select((m,i)=>new CuentaInicialDto(m.Nombre,m.Nombre=="Genesis"?"admin@example.invalid":$"maestra{i}@example.invalid")).ToList();
        var bad=roster.ToList();bad[0]=bad[0] with{Email=bad[1].Email};
        Check(await Controller().Import(bad) is BadRequestObjectResult && await db.Usuarios.CountAsync()==1,"Correos repetidos no crean cuentas");
        var attrs=typeof(CargaInicialController).GetCustomAttributes<AuthorizeAttribute>();
        Check(attrs.Any(x=>x.Roles=="Administradora"),"Importación restringida a administradora");
        Check(await Controller().Import(roster) is OkObjectResult,"Importación completa válida");
        Check(await db.Usuarios.CountAsync()==18,"18 personas sin duplicar la administradora Genesis");
        Check(await db.AsignacionesClaseMensuales.CountAsync()==12 && await db.Protocolos.CountAsync()==9,"Solo 12 clases y 9 domingos de noviembre y diciembre");
        Check(!await db.Protocolos.AnyAsync(x=>x.ResponsableMeriendaId!=null),"La importación no inventa responsables de merienda");
        var rebeca=await db.Usuarios.SingleAsync(x=>x.NombreCompleto=="Rebeca");
        Check(rebeca.Rol=="MaestraRotativa" && await db.Protocolos.AnyAsync(x=>x.Fecha==new DateOnly(2026,11,15)&&x.ResponsableProtocoloId==rebeca.Id) &&
            await db.AsignacionesClaseMensuales.AnyAsync(x=>x.Mes==12&&x.MaestraId==rebeca.Id),"Rebeca usa una cuenta para noviembre y diciembre");
        var hashes=await db.Usuarios.ToDictionaryAsync(x=>x.Id,x=>x.PasswordHash);
        Check(await Controller().Import(roster) is OkObjectResult && await db.Usuarios.CountAsync()==18 && await db.Protocolos.CountAsync()==9 &&
            (await db.Usuarios.ToListAsync()).All(x=>hashes[x.Id]==x.PasswordHash),"Reimportar no duplica personas ni cambia contraseñas");
        var group=await db.GruposClase.SingleAsync(x=>x.Nombre=="Cuna");
        var cal=new CalendariosController(db,new EmptyHub());
        Check(await cal.CrearAsignacion(new GuardarAsignacionClaseMensualDto{Anio=2026,Mes=11,GrupoClaseId=group.Id,MaestraId=rebeca.Id}) is BadRequestObjectResult,
            "Rebeca no puede dar clase el mes que tiene protocolo");
        var protocol=new ProtocoloController(db,new EmptyHub());
        Check(await protocol.Create(new GuardarProtocoloDto{Fecha=new DateOnly(2026,12,6),ResponsableMeriendaId=rebeca.Id}) is BadRequestObjectResult,
            "No se permite merienda a Rebeca en su mes de clases");
        var clock=new FixedClock(DateTimeOffset.Parse("2026-11-09T15:00:00Z"));
        var reminders=new ReminderService(db,clock);
        await reminders.GenerateAsync();
        Check(await db.Notificaciones.AnyAsync(x=>x.UsuarioId==rebeca.Id && x.Tipo=="Protocolo"),"La cuenta rotativa recibe su aviso de protocolo");
        clock.Now=DateTimeOffset.Parse("2026-11-30T15:00:00Z");await reminders.GenerateAsync();
        Check(await db.Notificaciones.AnyAsync(x=>x.UsuarioId==rebeca.Id && x.Tipo=="TurnoClase"),"La cuenta rotativa recibe clases al cambiar el mes");
        var admin=await db.Usuarios.SingleAsync(x=>x.Email=="admin@example.invalid");
        Check(await db.Notificaciones.AnyAsync(x=>x.UsuarioId==admin.Id && x.Tipo=="Protocolo"),"La administradora asignada recibe su aviso");
        var inactive=await db.Protocolos.SingleAsync(x=>x.Fecha==new DateOnly(2026,12,6));
        inactive.Activo=false;inactive.ResponsableMeriendaId=rebeca.Id;await db.SaveChangesAsync();
        Check(await Controller().Import(roster) is ConflictObjectResult && !inactive.Activo,
            "Reactivar protocolo no introduce un conflicto de merienda con clases");
        inactive.Activo=true;inactive.ResponsableMeriendaId=null;await db.SaveChangesAsync();
        // Cambiar responsable deliberadamente: la importación debe informar conflicto y conservarlo.
        var date=await db.Protocolos.SingleAsync(x=>x.Fecha==new DateOnly(2026,11,1));date.ResponsableProtocoloId=rebeca.Id;await db.SaveChangesAsync();
        Check(await Controller().Import(roster) is ConflictObjectResult && date.ResponsableProtocoloId==rebeca.Id,"Importar no sobrescribe un calendario ya modificado");
        return count;
    }
}
