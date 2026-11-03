using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.API.Services;
using NinosParaCristo.Domain.Entities;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Controllers;

[ApiController]
[Route("api/push")]
public class PushController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly WebPushNotificationService _push;

    public PushController(ApplicationDbContext context, WebPushNotificationService push)
    {
        _context = context;
        _push = push;
    }

    [HttpGet("public-key")]
    [AllowAnonymous]
    public ActionResult PublicKey()
    {
        if (string.IsNullOrWhiteSpace(_push.PublicKey))
            return Problem("Web Push todavía no está configurado en el servidor.");

        return Ok(new { publicKey = _push.PublicKey });
    }

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<ActionResult> Subscribe(PushSubscriptionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint) ||
            string.IsNullOrWhiteSpace(dto.P256dh) ||
            string.IsNullOrWhiteSpace(dto.Auth))
            return BadRequest(new { mensaje = "La suscripción Push está incompleta." });

        var uid = User.UserId();
        var row = await _context.PushSuscripciones.FirstOrDefaultAsync(x => x.Endpoint == dto.Endpoint);

        if (row is null)
        {
            row = new PushSuscripcion
            {
                UsuarioId = uid,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                Activa = true,
                FechaRegistro = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };
            _context.PushSuscripciones.Add(row);
        }
        else
        {
            row.UsuarioId = uid;
            row.P256dh = dto.P256dh;
            row.Auth = dto.Auth;
            row.UserAgent = Request.Headers["User-Agent"].ToString();
            row.Activa = true;
            row.FechaActualizacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Notificaciones activadas en este dispositivo." });
    }

    [HttpPost("unsubscribe")]
    [Authorize]
    public async Task<ActionResult> Unsubscribe(UnsubscribeDto dto)
    {
        var uid = User.UserId();
        var row = await _context.PushSuscripciones
            .FirstOrDefaultAsync(x => x.Endpoint == dto.Endpoint && x.UsuarioId == uid);

        if (row is not null)
        {
            row.Activa = false;
            row.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpPost("test")]
    [Authorize]
    public async Task<ActionResult> Test(CancellationToken cancellationToken)
    {
        var sent = await _push.SendToUserAsync(
            User.UserId(),
            "Niños para Cristo 🌈",
            "¡Listo! Las notificaciones están funcionando en este dispositivo.",
            "/#notificaciones",
            cancellationToken);

        if (sent == 0)
            return BadRequest(new { mensaje = "No hay una suscripción activa para este usuario. Activa primero las notificaciones." });

        return Ok(new { mensaje = "Notificación de prueba enviada.", dispositivos = sent });
    }
}

public record PushSubscriptionDto(string Endpoint, string P256dh, string Auth);
public record UnsubscribeDto(string Endpoint);
