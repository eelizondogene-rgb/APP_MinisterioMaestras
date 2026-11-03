using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NinosParaCristo.API.Hubs;

[Authorize]
public class UpdatesHub : Hub
{
}
