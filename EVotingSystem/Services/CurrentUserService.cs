using System.Security.Claims;

namespace EVotingSystem.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor)
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
    public string? FullName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);
}
