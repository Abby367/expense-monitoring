using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAFC.Expense.Application.Common.Interfaces;

namespace SAFC.Expense.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(IExpenseDbContext context) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
        => Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });

    [HttpGet("db")]
    public async Task<IActionResult> GetDb(CancellationToken cancellationToken)
    {
        try
        {
            await context.Users.CountAsync(cancellationToken);
            return Ok(new { status = "healthy", database = "reachable", timestamp = DateTimeOffset.UtcNow });
        }
        catch (Exception)
        {
            return StatusCode(503, new { status = "unhealthy", database = "unreachable", timestamp = DateTimeOffset.UtcNow });
        }
    }
}
