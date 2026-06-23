using System.Security.Claims;

namespace Idasletten.Shared;

/// <summary>Convenience accessor for the signed-in user, if any.</summary>
public class CurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public ClaimsPrincipal? Principal => _accessor.HttpContext?.User;
    public bool IsLoggedIn => Principal?.Identity?.IsAuthenticated == true;
    public string? Name => Principal?.Identity?.Name;
}
