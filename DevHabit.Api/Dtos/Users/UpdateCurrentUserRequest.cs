namespace DevHabit.Api.Dtos.Users;

/// <summary>
/// Data transfer object for updating current user
/// </summary>
public sealed record UpdateCurrentUserRequest
{
    /// <summary>
    /// The user's display name
    /// </summary>
    public required string Name { get; init; }
}
