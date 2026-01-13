namespace DevHabit.Api.Dtos.Entries;

public sealed record CreateEntryBatchRequest
{
    public required List<CreateEntryRequest> Entries { get; init; }
}
