using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("auth")]
[ApiVersion(1.0)]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext applicationIdentityDbContext,
    ApplicationDbContext applicationDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> jwtAuthOptions) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<TokenDto>> Register(
        [FromBody] RegisterUserDto registerUserDto,
        [FromServices] IValidator<RegisterUserDto> validator)
    {
        await validator.ValidateAndThrowAsync(registerUserDto);

        await using IDbContextTransaction transaction =
            await applicationIdentityDbContext.Database.BeginTransactionAsync();

        applicationDbContext.Database.SetDbConnection(applicationIdentityDbContext.Database.GetDbConnection());

        await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUserDto.Email,
            UserName = registerUserDto.Email
        };

        IdentityResult userIdentityResult = await userManager.CreateAsync(identityUser, registerUserDto.Password);

        if (!userIdentityResult.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Unable to register user",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", userIdentityResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
                });
        }

        IdentityResult roleIdentityResult = await userManager.AddToRoleAsync(identityUser, Roles.Member);

        if (!roleIdentityResult.Succeeded)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Unable to register user",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", roleIdentityResult.Errors.ToDictionary(x => x.Code, x => x.Description) }
                });
        }

        User user = registerUserDto.ToEntity();
        user.IdentityId = identityUser.Id;

        await applicationDbContext.Users.AddAsync(user);
        await applicationDbContext.SaveChangesAsync();

        TokenDto token = tokenProvider.Create(new GetTokenDto(identityUser.Id, identityUser.Email, [Roles.Member]));

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Value = token.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtAuthOptions.Value.RefreshTokenExpirationInDays)
        };

        await applicationIdentityDbContext.RefreshTokens.AddAsync(refreshToken);
        await applicationIdentityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(token);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenDto>> Login(
        [FromBody] LoginUserDto loginUserDto,
        [FromServices] IValidator<LoginUserDto> validator)
    {
        await validator.ValidateAndThrowAsync(loginUserDto);

        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(identityUser);

        TokenDto token = tokenProvider.Create(new GetTokenDto(identityUser.Id, identityUser.Email!, roles));

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Value = token.RefreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtAuthOptions.Value.RefreshTokenExpirationInDays)
        };

        await applicationIdentityDbContext.RefreshTokens.AddAsync(refreshToken);
        await applicationIdentityDbContext.SaveChangesAsync();

        return Ok(token);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenDto>> Refresh(
        [FromBody] RefreshTokenDto refreshTokenDto,
        [FromServices] IValidator<RefreshTokenDto> validator)
    {
        await validator.ValidateAndThrowAsync(refreshTokenDto);

        RefreshToken? refreshToken = await applicationIdentityDbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Value == refreshTokenDto.Value);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        IList<string> roles = await userManager.GetRolesAsync(refreshToken.User);

        TokenDto token = tokenProvider.Create(new GetTokenDto(refreshToken.User.Id, refreshToken.User.Email!, roles));

        refreshToken.Value = token.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtAuthOptions.Value.RefreshTokenExpirationInDays);

        await applicationIdentityDbContext.SaveChangesAsync();

        return Ok(token);
    }
}
