using Microsoft.Extensions.Logging;
using Rise.Persistence;

public class HealthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<HealthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryService"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="validationService">The validation service.</param>
    public HealthService(ApplicationDbContext dbContext, ILogger<HealthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CheckDatabaseConnection()
    {
        await _dbContext.Database.CanConnectAsync();
    }
}