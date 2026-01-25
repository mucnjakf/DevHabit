using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Services;
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
    RefitGitHubService gitHubService,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    [HttpPut("personal-access-token")]
    public async Task<ActionResult> StoreAccessToken(
        [FromBody] StoreGitHubPatRequest storeGitHubPatRequest,
        [FromServices] IValidator<StoreGitHubPatRequest> validator)
    {
        await validator.ValidateAndThrowAsync(storeGitHubPatRequest);

        string userId = (await userContext.GetUserIdAsync())!;

        await gitHubPatService.StoreAsync(userId!, storeGitHubPatRequest);

        return NoContent();
    }

    [HttpDelete("personal-access-token")]
    public async Task<ActionResult> RevokeAccessToken()
    {
        string userId = (await userContext.GetUserIdAsync())!;

        await gitHubPatService.RevokeAsync(userId!);

        return NoContent();
    }

    [HttpGet("profile")]
    public async Task<ActionResult<GitHubUserProfileDto>> GetUserProfile(
        [FromHeader(Name = "Accept")] string? accept)
    {
        string userId = (await userContext.GetUserIdAsync())!;

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

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<GitHubEventDto>>> GetUserEvents()
    {
        string? userId = await userContext.GetUserIdAsync();

        string? token = await gitHubPatService.GetAsync(userId!);

        if (token is null)
        {
            return Unauthorized();
        }

        GitHubUserProfileDto? profile = await gitHubService.GetUserProfileAsync(token);

        if (profile is null)
        {
            return NotFound();
        }

        IReadOnlyList<GitHubEventDto>? events = await gitHubService
            .GetUserEventsAsync(profile.Login, token);

        if (events is null)
        {
            return NotFound();
        }

        return Ok(events);
    }
}
