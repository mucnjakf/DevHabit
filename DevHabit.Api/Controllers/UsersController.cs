using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("users")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
public sealed class UsersController(ApplicationDbContext dbContext, UserContext userContext, LinkService linkService)
    : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<UserDto>> GetUserById([FromRoute] string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        UserDto? user = await dbContext.Users
            .Where(x => x.Id == id)
            .Select(UserProjections.ProjectToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser([FromHeader(Name = "Accept")] string accept)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(UserProjections.ProjectToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            user.Links =
            [
                linkService.Create(nameof(GetCurrentUser), "self", HttpMethods.Get),
                linkService.Create(nameof(UpdateCurrentUser), "update-current-user", HttpMethods.Put)
            ];
        }

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult> UpdateCurrentUser([FromBody] UpdateCurrentUserDto updateCurrentUserDto)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return NotFound();
        }

        user.Name = updateCurrentUserDto.Name;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
