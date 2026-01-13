using FluentValidation;

namespace DevHabit.Api.Dtos.Entries;

public sealed class CreateEntryBatchRequestValidator : AbstractValidator<CreateEntryBatchRequest>
{
    public CreateEntryBatchRequestValidator(CreateEntryRequestValidator entryValidator)
    {
        RuleFor(x => x.Entries)
            .NotEmpty()
            .WithMessage("At least one entry is required")
            .Must(x => x.Count <= 20)
            .WithMessage("Maximum of 20 entries per batch");

        RuleForEach(x => x.Entries)
            .SetValidator(entryValidator);
    }
}
