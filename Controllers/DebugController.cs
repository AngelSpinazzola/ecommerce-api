using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DebugController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var jwtSecret = _configuration["Jwt:SecretKey"];
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            return Ok(new
            {
                status = "OK",
                environment = environment,
                hasConnectionString = !string.IsNullOrEmpty(connectionString),
                connectionStringLength = connectionString?.Length ?? 0,
                connectionStringStart = connectionString?.Substring(0, Math.Min(20, connectionString?.Length ?? 0)) + "...",
                hasJwtSecret = !string.IsNullOrEmpty(jwtSecret),
                jwtSecretLength = jwtSecret?.Length ?? 0,
                timestamp = DateTime.UtcNow,
                allConfigKeys = _configuration.AsEnumerable()
                    .Where(x => x.Key.Contains("Connection") || x.Key.Contains("Jwt"))
                    .Select(x => new { key = x.Key, hasValue = !string.IsNullOrEmpty(x.Value) })
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("env-vars")]
    public IActionResult GetEnvVars()
    {
        try
        {
            var envVars = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Where(x => x.Key.ToString().Contains("Connection") ||
                           x.Key.ToString().Contains("Jwt") ||
                           x.Key.ToString().Contains("ASPNETCORE"))
                .Select(x => new {
                    key = x.Key.ToString(),
                    hasValue = !string.IsNullOrEmpty(x.Value?.ToString()),
                    valueLength = x.Value?.ToString()?.Length ?? 0
                })
                .ToList();

            return Ok(new { environmentVariables = envVars, timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
