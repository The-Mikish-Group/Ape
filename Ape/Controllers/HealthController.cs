using Ape.Data;
using Ape.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ape.Controllers
{
    /// <summary>
    /// Health check endpoint for monitoring application status.
    /// Returns JSON with status of database, email, and encryption configuration.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HealthController(
        ApplicationDbContext context,
        CredentialEncryptionService encryptionService,
        SecureConfigurationService configService,
        ILogger<HealthController> logger) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly CredentialEncryptionService _encryptionService = encryptionService;
        private readonly SecureConfigurationService _configService = configService;
        private readonly ILogger<HealthController> _logger = logger;

        /// <summary>
        /// GET /health - Returns overall system health status
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var health = new HealthCheckResult
            {
                Timestamp = DateTime.UtcNow,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0"
            };

            // Check database connectivity
            health.Database = await CheckDatabaseAsync();

            // Check encryption key configuration
            health.Encryption = CheckEncryption();

            // Check email configuration
            health.Email = await CheckEmailConfigurationAsync();

            // Determine overall status
            health.Status = DetermineOverallStatus(health);

            var statusCode = health.Status == "Healthy" ? 200 :
                             health.Status == "Degraded" ? 200 : 503;

            return StatusCode(statusCode, health);
        }

        /// <summary>
        /// GET /health/live - Kubernetes liveness probe (is the app running?)
        /// </summary>
        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// GET /health/ready - Kubernetes readiness probe (is the app ready to serve traffic?)
        /// </summary>
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            var dbHealthy = await CheckDatabaseAsync();

            if (dbHealthy.Status == "Healthy")
            {
                return Ok(new { status = "Ready", timestamp = DateTime.UtcNow });
            }

            return StatusCode(503, new { status = "Not Ready", reason = dbHealthy.Message, timestamp = DateTime.UtcNow });
        }

        private async Task<ComponentHealth> CheckDatabaseAsync()
        {
            var component = new ComponentHealth { Name = "Database" };

            try
            {
                // Test actual database connectivity
                var canConnect = await _context.Database.CanConnectAsync();

                if (canConnect)
                {
                    // Also verify we can query (not just connect)
                    var userCount = await _context.Users.CountAsync();
                    component.Status = "Healthy";
                    component.Message = $"Connected. {userCount} users in database.";
                }
                else
                {
                    component.Status = "Unhealthy";
                    component.Message = "Cannot connect to database.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                component.Status = "Unhealthy";
                component.Message = $"Database error: {ex.Message}";
            }

            return component;
        }

        private ComponentHealth CheckEncryption()
        {
            var component = new ComponentHealth { Name = "Encryption" };

            try
            {
                if (_encryptionService.IsMasterKeyConfigured())
                {
                    // Test encryption/decryption round-trip
                    var testValue = "health-check-test";
                    var encrypted = _encryptionService.Encrypt(testValue);
                    var decrypted = _encryptionService.Decrypt(encrypted);

                    if (decrypted == testValue)
                    {
                        component.Status = "Healthy";
                        component.Message = "Master key configured and encryption working.";
                    }
                    else
                    {
                        component.Status = "Unhealthy";
                        component.Message = "Encryption round-trip failed.";
                    }
                }
                else
                {
                    component.Status = "Degraded";
                    component.Message = "Master encryption key not configured. Set MASTER_CREDENTIAL_KEY_ILLUSTRATE environment variable.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption health check failed");
                component.Status = "Unhealthy";
                component.Message = $"Encryption error: {ex.Message}";
            }

            return component;
        }

        private async Task<ComponentHealth> CheckEmailConfigurationAsync()
        {
            var component = new ComponentHealth { Name = "Email" };

            try
            {
                // Check if email credentials are configured
                var smtpHost = await _configService.GetCredentialAsync("SmtpHost");
                var azureConnectionString = await _configService.GetCredentialAsync("AzureEmailConnectionString");

                var hasSmtp = !string.IsNullOrEmpty(smtpHost);
                var hasAzure = !string.IsNullOrEmpty(azureConnectionString);

                if (hasSmtp && hasAzure)
                {
                    component.Status = "Healthy";
                    component.Message = "Both SMTP and Azure Email configured (dual-provider).";
                }
                else if (hasSmtp || hasAzure)
                {
                    component.Status = "Healthy";
                    component.Message = hasAzure ? "Azure Email configured." : "SMTP configured.";
                }
                else
                {
                    component.Status = "Degraded";
                    component.Message = "No email provider configured. Email functionality unavailable.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email configuration check failed");
                component.Status = "Degraded";
                component.Message = "Could not verify email configuration.";
            }

            return component;
        }

        private static string DetermineOverallStatus(HealthCheckResult health)
        {
            var statuses = new[] { health.Database.Status, health.Encryption.Status, health.Email.Status };

            if (statuses.Any(s => s == "Unhealthy"))
                return "Unhealthy";

            if (statuses.Any(s => s == "Degraded"))
                return "Degraded";

            return "Healthy";
        }
    }

    /// <summary>
    /// Overall health check result
    /// </summary>
    public class HealthCheckResult
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = "1.0.0";
        public ComponentHealth Database { get; set; } = new();
        public ComponentHealth Encryption { get; set; } = new();
        public ComponentHealth Email { get; set; } = new();
    }

    /// <summary>
    /// Individual component health status
    /// </summary>
    public class ComponentHealth
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Unknown";
        public string Message { get; set; } = string.Empty;
    }
}
