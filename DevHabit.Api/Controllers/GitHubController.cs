using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Hateoas;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("github")]
[ApiVersion(1.0)]
[Authorize(Roles = Roles.Member)]
public sealed class GitHubController(
    GitHubPatService gitHubPatService,
    GitHubService gitHubService,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    [HttpPut("personal-access-token")]
    public async Task<ActionResult> StoreAccessToken(
        [FromBody] StoreGitHubPatRequest storeGitHubPatRequest,
        [FromServices] IValidator<StoreGitHubPatRequest> validator)
    {
        await validator.ValidateAndThrowAsync(storeGitHubPatRequest);

        string userId = await userContext.GetUserIdAsync();

        await gitHubPatService.StoreAsync(userId!, storeGitHubPatRequest);

        return NoContent();
    }

    [HttpDelete("personal-access-token")]
    public async Task<ActionResult> RevokeAccessToken()
    {
        string userId = await userContext.GetUserIdAsync();

        await gitHubPatService.RevokeAsync(userId!);

        return NoContent();
    }

    // TODO: return response object
    [HttpGet("profile")]
    public async Task<ActionResult<GitHubUserProfileDto>> GetUserProfile(
        [FromHeader(Name = "Accept")] string? accept)
    {
        string userId = await userContext.GetUserIdAsync();

        string? gitHubPat = await gitHubPatService.GetAsync(userId!);

        if (string.IsNullOrWhiteSpace(gitHubPat))
        {
            return NotFound();
        }

        GitHubUserProfileDto? gitHubUserProfileDto = await gitHubService.GetUserProfileAsync(gitHubPat);

        if (gitHubUserProfileDto is null)
        {
            return NotFound();
        }

        if (accept is VendorMediaTypeNames.Application.HateoasJson)
        {
            gitHubUserProfileDto.Links =
            [
                linkService.Create(nameof(GetUserProfile), "self", HttpMethods.Get),
                linkService.Create(nameof(StoreAccessToken), "store-access-token", HttpMethods.Put),
                linkService.Create(nameof(RevokeAccessToken), "revoke-access-token", HttpMethods.Delete)
            ];
        }

        return Ok(gitHubUserProfileDto);
    }
}
