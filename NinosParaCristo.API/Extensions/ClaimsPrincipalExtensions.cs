using System.Security.Claims;

namespace NinosParaCristo.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int UserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}
