using System.Security.Claims;

namespace DevHabit.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal? claimsPrincipal)
    {
        public string? GetIdentityId()
            => claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
