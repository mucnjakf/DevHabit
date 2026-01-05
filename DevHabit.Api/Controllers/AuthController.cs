using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext applicationIdentityDbContext,
    ApplicationDbContext applicationDbContext) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserDto registerUserDto)
    {
        await using IDbContextTransaction transaction =
            await applicationIdentityDbContext.Database.BeginTransactionAsync();

        applicationDbContext.Database.SetDbConnection(applicationIdentityDbContext.Database.GetDbConnection());

        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        IdentityResult identityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!identityResult.Succeeded)
        {
            return Problem(detail: "Unable to register user", statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    { "errors", identityResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
                });
        }

        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        await applicationDbContext.Users.AddAsync(user);
        await applicationDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(user.Id);
    }
}
