using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>Liveness probe. Unversioned by convention. The timestamp is UTC (serialized with a 'Z').</summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public HealthStatus Get() => new("Healthy", DateTime.UtcNow);
}

/// <summary>Health payload. <see cref="TimeUtc"/> is always UTC — the API exposes no local times.</summary>
public sealed record HealthStatus(string Status, DateTime TimeUtc);
