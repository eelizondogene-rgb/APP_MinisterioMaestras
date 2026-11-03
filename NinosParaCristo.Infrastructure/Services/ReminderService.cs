using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.Infrastructure.Services;

public class ReminderService
{
    private readonly ApplicationDbContext _context;
    public ReminderService(ApplicationDbContext context) => _context = context;

    public async Task<int> GenerateAsync()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var targetDate = hoy.AddDays(7);
        var created = 0;

        // Las clases se organizan por mes y grupo. Como el ministerio trabaja los domingos,
        // el recordatorio de clase se genera exactamente una semana antes de cada domingo.
        if (targetDate.DayOfWeek == DayOfWeek.Sunday)
        {
            var asignaciones = await _context.AsignacionesClaseMensuales.AsNoTracking()
                .Include(x => x.GrupoClase)
                .Where(x => x.Activo && x.Anio == targetDate.Year && x.Mes == targetDate.Month && x.GrupoClase.Activo)
                .ToListAsync();

            foreach (var a in asignaciones)
            {
                var key = $"clase:{a.Id}:{targetDate:yyyyMMdd}:7d";
                if (await _context.Notificaciones.AnyAsync(x => x.ClaveUnica == key)) continue;

                var material = string.IsNullOrWhiteSpace(a.MaterialUrl)
                    ? "El material todavía no tiene enlace asignado."
                    : $"Material: {a.MaterialUrl}";

                _context.Notificaciones.Add(new Notificacion
                {
                    UsuarioId = a.MaestraId,
                    Titulo = $"Clase de {a.GrupoClase.Nombre} · próximo domingo",
                    Mensaje = $"El domingo {targetDate:dd/MM/yyyy} tienes asignado el grupo {a.GrupoClase.Nombre} ({a.GrupoClase.EdadMinima}-{a.GrupoClase.EdadMaxima} años). {material}",
                    Tipo = "TurnoClase",
                    FechaProgramada = DateTime.UtcNow,
                    FechaCreacion = DateTime.UtcNow,
                    Enviada = false,
                    ClaveUnica = key
                });
                created++;
            }
        }

        var protocolos = await _context.Protocolos.AsNoTracking()
            .Where(x => x.Activo && x.Fecha == targetDate).ToListAsync();

        foreach (var p in protocolos)
        {
            if (p.ResponsableMeriendaId.HasValue)
            {
                var key = $"merienda:{p.Id}:7d";
                if (!await _context.Notificaciones.AnyAsync(x => x.ClaveUnica == key))
                {
                    _context.Notificaciones.Add(new Notificacion
                    {
                        UsuarioId = p.ResponsableMeriendaId.Value,
                        Titulo = "Recordatorio de merienda",
                        Mensaje = $"El {p.Fecha:dd/MM/yyyy} tienes asignada la merienda: {p.Merienda ?? "por definir"}.",
                        Tipo = "Merienda",
                        FechaProgramada = DateTime.UtcNow,
                        FechaCreacion = DateTime.UtcNow,
                        Enviada = false,
                        ClaveUnica = key
                    });
                    created++;
                }
            }

            if (p.ResponsableProtocoloId.HasValue)
            {
                var key = $"protocolo:{p.Id}:7d";
                if (!await _context.Notificaciones.AnyAsync(x => x.ClaveUnica == key))
                {
                    _context.Notificaciones.Add(new Notificacion
                    {
                        UsuarioId = p.ResponsableProtocoloId.Value,
                        Titulo = "Recordatorio de protocolo",
                        Mensaje = $"El {p.Fecha:dd/MM/yyyy} tienes tareas de protocolo: {p.Tareas ?? "limpieza de pasillos y baños, recepción de la ofrenda y entrega de la merienda"}.",
                        Tipo = "Protocolo",
                        FechaProgramada = DateTime.UtcNow,
                        FechaCreacion = DateTime.UtcNow,
                        Enviada = false,
                        ClaveUnica = key
                    });
                    created++;
                }
            }
        }

        if (created > 0) await _context.SaveChangesAsync();
        return created;
    }
}
