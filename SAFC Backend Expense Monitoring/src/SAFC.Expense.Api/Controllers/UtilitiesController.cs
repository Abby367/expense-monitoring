using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SAFC.Expense.Api.Options;
using SAFC.Expense.Application.Utilities.SeedDefaults;

namespace SAFC.Expense.Api.Controllers;

[ApiController]
[Route("api/v1/utilities")]
[AllowAnonymous]
public sealed class UtilitiesController(
    SeedDefaultsHandler handler,
    IOptions<UtilitiesOptions> utilitiesOptions,
    ILogger<UtilitiesController> logger) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDefaults(
        [FromBody] SeedRequest request,
        CancellationToken cancellationToken)
    {
        var refusal = CheckConfirmation(
            request.Confirm, utilitiesOptions.Value.SeedDefaultsConfirmPhrase);

        if (refusal is not null)
            return refusal;
        
        var response = await handler.Handle(
            new SeedDefaultsCommand(request.Apply, utilitiesOptions.Value.SuperAdminEmail),
            cancellationToken);


        if (response.Applied)
        {
            logger.LogInformation(
                "Seed applied: {PermissionsCreated} permissions created, {PermissionsUpdated} updated, {BranchesCreated} branches created, {BranchesUpdated} updated. Keys: {Keys}. Codes: {Codes}.",
                response.Permissions.Created.Count, response.Permissions.Updated.Count,
                response.Branches.Created.Count, response.Branches.Updated.Count,
                response.Permissions.Created, response.Branches.Created);

            // The only audit trail for a privilege grant: GrantedById is null on the row, so the
            // database records no actor. Warning, not Information, because an applied seed run
            // that touches the SUPERADMIN grant is worth seeing even when the outcome is a refusal.
            logger.LogWarning(
                "Seed grant for {Email}: {Outcome}. Caller {RemoteIp}.",
                response.SuperAdminGrant.Email,
                response.SuperAdminGrant.Outcome,
                HttpContext.Connection.RemoteIpAddress);
        }

        else
            logger.LogDebug("Seed preview requested.");

        return Ok(response);
    }

    private ObjectResult? CheckConfirmation(string? submitted, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            logger.LogWarning(
                "Seed refused: {Setting} is not configured. Caller {RemoteIp}.",
                "Utilities__SeedDefaultsConfirmPhrase", HttpContext.Connection.RemoteIpAddress);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Seeding is not configured on this deployment." });
        }

        if (!string.Equals(submitted, expected, StringComparison.Ordinal))
        {
           
            logger.LogWarning(
                "Seed refused: confirmation phrase did not match. Caller {RemoteIp}.",
                HttpContext.Connection.RemoteIpAddress);

            return BadRequest(new { error = "Confirmation phrase does not match." });
        }

        return null;
    }
}


public sealed record SeedRequest(string? Confirm, bool Apply = false);
