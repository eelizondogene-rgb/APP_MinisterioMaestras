using Microsoft.AspNetCore.SignalR;
using NinosParaCristo.API.Hubs;
using NinosParaCristo.Infrastructure.Services;

namespace NinosParaCristo.API.Services;

public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<UpdatesHub> _hub;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(IServiceScopeFactory scopeFactory, IHubContext<UpdatesHub> hub, ILogger<ReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<ReminderService>();
                var push = scope.ServiceProvider.GetRequiredService<WebPushNotificationService>();
                var created = await reminders.GenerateAsync();
                var pushed = await push.SendPendingAsync(stoppingToken);
                if (created > 0 || pushed > 0)
                    await _hub.Clients.All.SendAsync("actualizacion", "notificaciones", cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando recordatorios.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
