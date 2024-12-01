namespace Rise.Server.LoggingEnrichers;

public class ClientIpAddressEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientIpAddressEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Enrich()
    {
        return _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
