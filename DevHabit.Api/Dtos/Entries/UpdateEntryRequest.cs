namespace DevHabit.Api.Dtos.Entries;

public sealed record UpdateEntryRequest
{
    public required int Value { get; init; }

    public string? Notes { get; init; }
}
