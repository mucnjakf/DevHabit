using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Entries;

[ValidateNever]
public sealed record CreateEntryImportJobRequest
{
    public required IFormFile File { get; init; }
}
