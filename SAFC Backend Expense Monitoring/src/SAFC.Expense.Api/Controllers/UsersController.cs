using Microsoft.AspNetCore.Mvc;
using SAFC.Expense.Application.Users.CreateUser;
using Microsoft.AspNetCore.Authorization;  

namespace SAFC.Expense.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[AllowAnonymous]
public sealed class UsersController(
    CreateUserHandler handler,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        // TODO: remove both of these when JWT auth lands (epic 4)
        if (!environment.IsDevelopment())
            return NotFound();

        var response = await handler.Handle(command, cancellationToken);
        return Created($"/api/v1/users/{response.Id}", response);
    }
}
