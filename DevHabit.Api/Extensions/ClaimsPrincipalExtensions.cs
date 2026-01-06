using System.Security.Claims;

namespace DevHabit.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetIdentityId(this ClaimsPrincipal? claimsPrincipal)
        => claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
}
