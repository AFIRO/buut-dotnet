using System.Security.Claims;

namespace Rise.Server.LoggingEnrichers;

public class UserIdEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Enrich()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
    }
}
