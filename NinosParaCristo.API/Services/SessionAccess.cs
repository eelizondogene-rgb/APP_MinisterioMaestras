using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using NinosParaCristo.API.Extensions;
using NinosParaCristo.Infrastructure.Persistence;

namespace NinosParaCristo.API.Services;

public static class SessionAccess
{
    public static Task<bool> IsCurrentAsync(ApplicationDbContext db, ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var id = principal.UserId();
        var role = principal.FindFirstValue(ClaimTypes.Role);
        return db.Usuarios.AsNoTracking().AnyAsync(
            user => user.Id == id && user.Activo && user.Rol == role, cancellationToken);
    }
}
