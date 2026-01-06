using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext applicationIdentityDbContext,
    ApplicationDbContext applicationDbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> jwtAuthOptions) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<TokenDto>> Register(RegisterUserDto registerUserDto)
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

        TokenDto token = tokenProvider.Create(new GetTokenDto(identityUser.Id, identityUser.Email));

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
    public async Task<ActionResult<TokenDto>> Login(LoginUserDto loginUserDto)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUserDto.Email);

        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, loginUserDto.Password))
        {
            return Unauthorized();
        }

        TokenDto token = tokenProvider.Create(new GetTokenDto(identityUser.Id, identityUser.Email!));

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
    public async Task<ActionResult<TokenDto>> Refresh(RefreshTokenDto refreshTokenDto)
    {
        RefreshToken? refreshToken = await applicationIdentityDbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Value == refreshTokenDto.Value);

        if (refreshToken is null || refreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        TokenDto token = tokenProvider.Create(new GetTokenDto(refreshToken.User.Id, refreshToken.User.Email!));

        refreshToken.Value = token.RefreshToken;
        refreshToken.ExpiresAtUtc = DateTime.UtcNow.AddDays(jwtAuthOptions.Value.RefreshTokenExpirationInDays);

        await applicationIdentityDbContext.SaveChangesAsync();

        return Ok(token);
    }
}
