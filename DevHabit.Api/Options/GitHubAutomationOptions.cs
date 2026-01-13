namespace DevHabit.Api.Options;

public sealed class GitHubAutomationOptions
{
    public const string Section = "GitHubAutomation";

    public required int ScanIntervalInMinutes { get; init; }
}
