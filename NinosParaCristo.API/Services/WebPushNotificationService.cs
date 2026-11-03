using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Infrastructure.Persistence;
using WebPush;

namespace NinosParaCristo.API.Services;

public class WebPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public string PublicKey => _configuration["WebPush:PublicKey"] ?? string.Empty;

    public async Task<int> SendToUserAsync(
        int usuarioId,
        string titulo,
        string mensaje,
        string url = "/",
        CancellationToken cancellationToken = default)
    {
        var publicKey = _configuration["WebPush:PublicKey"];
        var privateKey = _configuration["WebPush:PrivateKey"];
        var subject = _configuration["WebPush:Subject"] ?? "mailto:admin@ninosparacristo.local";

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            _logger.LogWarning("Web Push no está configurado. Faltan las claves VAPID.");
            return 0;
        }

        var subscriptions = await _context.PushSuscripciones
            .Where(x => x.UsuarioId == usuarioId && x.Activa)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
            return 0;

        var payload = JsonSerializer.Serialize(new
        {
            title = titulo,
            body = mensaje,
            icon = "/assets/icon-192.png",
            badge = "/assets/badge-96.png",
            url
        });

        var vapid = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();
        var sent = 0;

        foreach (var item in subscriptions)
        {
            try
            {
                var subscription = new PushSubscription(item.Endpoint, item.P256dh, item.Auth);
                await client.SendNotificationAsync(subscription, payload, vapid, cancellationToken);
                sent++;
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                item.Activa = false;
                item.FechaActualizacion = DateTime.UtcNow;
                _logger.LogInformation("Suscripción Web Push vencida; se desactivó para usuario {UsuarioId}.", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar Web Push al usuario {UsuarioId}.", usuarioId);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return sent;
    }

    public async Task<int> SendPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _context.Notificaciones
            .Where(x => !x.Enviada && x.FechaProgramada <= DateTime.UtcNow)
            .OrderBy(x => x.FechaProgramada)
            .Take(100)
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var notification in pending)
        {
            var sent = await SendToUserAsync(
                notification.UsuarioId,
                notification.Titulo,
                notification.Mensaje,
                "/#notificaciones",
                cancellationToken);

            if (sent > 0)
            {
                notification.Enviada = true;
                total += sent;
            }
        }

        if (pending.Count > 0)
            await _context.SaveChangesAsync(cancellationToken);

        return total;
    }
}
