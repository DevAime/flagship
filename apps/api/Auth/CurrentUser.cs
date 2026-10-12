using System.Security.Claims;

namespace FlagForge.Api.Auth;

public interface ICurrentUser
{
    Guid? Id { get; }
    string Email { get; }
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? Id =>
        Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? id : null;

    public string Email => Principal?.FindFirst("email")?.Value ?? "system";
}
