using System.Net.Mime;
using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("users")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
[Produces(
    MediaTypeNames.Application.Json,
    VendorMediaTypeNames.Application.JsonV1,
    VendorMediaTypeNames.Application.HateoasJson,
    VendorMediaTypeNames.Application.HateoasJsonV1)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class UsersController(
    DevHabitDbContext dbContext,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">The user's unique identifier</param>
    /// <returns>The user details</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById([FromRoute] string id)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        if (id != userId)
        {
            return Forbid();
        }

        UserDto? user = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(UserProjections.ProjectToDto())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Gets the currently authenticated user.
    /// </summary>
    /// <param name="accept">Accept header from header</param>
    /// <returns>The current user's details</returns>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetCurrentUser([FromHeader(Name = "Accept")] string accept)
    {
        string userId = (await userContext.GetUserIdAsync())!;

        UserDto? user = await dbContext.Users
            .AsNoTracking()
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

    /// <summary>
    /// Updates the current user.
    /// </summary>
    /// <param name="updateCurrentUserRequest">The update details</param>
    /// <param name="validator">Validator for update request</param>
    /// <returns>No content on success</returns>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateCurrentUser(
        [FromBody] UpdateCurrentUserRequest updateCurrentUserRequest,
        [FromServices] IValidator<UpdateCurrentUserRequest> validator)
    {
        await validator.ValidateAndThrowAsync(updateCurrentUserRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        User? user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
        {
            return NotFound();
        }

        user.Name = updateCurrentUserRequest.Name;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}
