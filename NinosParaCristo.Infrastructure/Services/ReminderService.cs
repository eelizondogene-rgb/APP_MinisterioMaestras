using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Application.Common;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.Infrastructure.Services;

public class ReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _clock;
    public ReminderService(ApplicationDbContext context, TimeProvider? clock = null)
    { _context = context; _clock = clock ?? TimeProvider.System; }

    // Costa Rica utiliza UTC-6 todo el año. No depende de la zona del servidor.
    public static DateOnly? TargetSunday(DateTimeOffset utcNow)
    {
        var local = utcNow.ToOffset(TimeSpan.FromHours(-6));
        if (local.DayOfWeek == DayOfWeek.Sunday ||
            (local.DayOfWeek == DayOfWeek.Monday && local.Hour < 9)) return null;
        return DateOnly.FromDateTime(local.DateTime).AddDays(7 - (int)local.DayOfWeek);
    }

    public async Task<int> GenerateAsync()
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var target = TargetSunday(_clock.GetUtcNow());
        var desired = new Dictionary<string, Notificacion>();
        void Add(string key, int userId, string title, string message, string type)
        {
            desired[key] = new Notificacion {
                ClaveUnica=key, UsuarioId=userId, Titulo=title, Mensaje=message.Length<=1000?message:"Tienes una asignación para el próximo domingo. Consulta todos los detalles en tu calendario.",
                Tipo=type, FechaProgramada=now, FechaCreacion=now, Enviada=false
            };
        }
        if (target is DateOnly date)
        {
            var assignments = await _context.AsignacionesClaseMensuales.AsNoTracking()
                .Include(x=>x.GrupoClase).Include(x=>x.Maestra)
                .Where(x=>x.Activo && x.Anio==date.Year && x.Mes==date.Month &&
                    x.GrupoClase.Activo && x.Maestra.Activo && (x.Maestra.Rol==Roles.MaestraClase || x.Maestra.Rol==Roles.MaestraRotativa || x.Maestra.Rol==Roles.Administradora)).ToListAsync();
            foreach (var a in assignments)
            {
                var material=string.IsNullOrWhiteSpace(a.MaterialUrl)?"Material pendiente.":$"Material: {a.MaterialUrl}";
                Add($"lunes:clase:{a.Id}:{a.MaestraId}:{date:yyyyMMdd}",a.MaestraId,
                    $"Clase de {a.GrupoClase.Nombre} · próximo domingo",
                    $"El domingo {date:dd/MM/yyyy} tienes asignado el grupo {a.GrupoClase.Nombre}. {material}","TurnoClase");
            }
            var staff = await _context.Usuarios.AsNoTracking()
                .Where(x=>x.Activo && (x.Rol==Roles.MaestraProtocolo || x.Rol==Roles.MaestraRotativa || x.Rol==Roles.Administradora)).Select(x=>x.Id).ToListAsync();
            var protocols=await _context.Protocolos.AsNoTracking().Where(x=>x.Activo && x.Fecha==date).ToListAsync();
            foreach(var p in protocols)
            {
                if(p.ResponsableMeriendaId is int snack && staff.Contains(snack))
                    Add($"lunes:merienda:{p.Id}:{snack}:{date:yyyyMMdd}",snack,"Recordatorio de merienda",
                        $"El domingo {date:dd/MM/yyyy} tienes asignada la merienda: {p.Merienda ?? "por definir"}.","Merienda");
                if(p.ResponsableProtocoloId is int protocol && staff.Contains(protocol))
                    Add($"lunes:protocolo:{p.Id}:{protocol}:{date:yyyyMMdd}",protocol,"Recordatorio de protocolo",
                        $"El domingo {date:dd/MM/yyyy} tienes tareas de protocolo: {p.Tareas ?? "por definir"}.","Protocolo");
            }
        }

        // Conservar historial enviado; retirar pendientes cancelados, vencidos o de la versión anterior.
        var managed=await _context.Notificaciones.Where(x=>x.ClaveUnica!=null &&
            (x.ClaveUnica.StartsWith("lunes:") || x.ClaveUnica.StartsWith("clase:") ||
             x.ClaveUnica.StartsWith("merienda:") || x.ClaveUnica.StartsWith("protocolo:"))).ToListAsync();
        foreach(var old in managed.Where(x=>!x.Enviada && !desired.ContainsKey(x.ClaveUnica!)))
            _context.Notificaciones.Remove(old);
        var count=0;
        foreach(var pair in desired)
        {
            var existing=managed.FirstOrDefault(x=>x.ClaveUnica==pair.Key);
            if(existing is null) { _context.Notificaciones.Add(pair.Value); count++; }
            else if(existing.Mensaje!=pair.Value.Mensaje || existing.Titulo!=pair.Value.Titulo)
            {
                existing.Mensaje=pair.Value.Mensaje; existing.Titulo=pair.Value.Titulo;
                existing.Enviada=false; existing.Leida=false; existing.FechaProgramada=now;
                count++;
            }
        }
        await _context.SaveChangesAsync();
        return count;
    }
}
