using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rise.Server.Controllers{
    /// <summary>
    /// Controller to handle health check endpoints.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly HealthService _healthService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance to log information.</param>
        public HealthController(HealthService healthService, ILogger<HealthController> logger)
        {
            _logger = logger;
            _healthService = healthService;
        }

        
        /// <summary>
        /// Health check endpoint to verify if the API is ready.
        /// </summary>
        /// <returns>Returns an Ok result if the API is ready.</returns>
        /// <response code="200">returns ok</response>
        [HttpGet("/health/open/apiReady")]
        public async Task<ActionResult> OpenGet()
        {
            _logger.LogInformation("public Health check endpoint called");
            await Task.CompletedTask; // Simulate async work
            return Ok();
        }

        /// <summary>
        /// Health check endpoint to verify if the API is ready and the database is accessible.
        /// </summary>
        /// <returns>Returns an Ok result if the API and database are ready.</returns>
        /// <response code="200">Returns ok</response>
        /// <response code="500">Returns internal server error if database is not reachable</response>
        [HttpGet("/health/open/dbStatus")]
        public async Task<ActionResult> OpenDbStatusGet()
        {
            _logger.LogInformation("Health check endpoint called");

            try
            {
                await _healthService.CheckDatabaseConnection();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");

                // If an exception occurs, return a 500 Internal Server Error
                return StatusCode(500, "Database is not reachable");
            }
        }


        /// <summary>
        /// Health check endpoint to verify if the API is ready.
        /// </summary>
        /// <returns>Returns an Ok result if the API is ready.</returns>
        /// <response code="200">returns ok</response>
        /// <response code="401">when not authorized in the correct role</response>
        [HttpGet("/health/admin/apiReady")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAdmin()
        {
            _logger.LogInformation("Admin Health check endpoint called");
            await Task.CompletedTask; // Simulate async work
            return Ok();
        }
    }
}