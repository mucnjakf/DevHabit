using FluentValidation;

namespace DevHabit.Api.Dtos.HabitTags;

public sealed class UpsertHabitTagsRequestValidator : AbstractValidator<UpsertHabitTagsRequest>
{
    public UpsertHabitTagsRequestValidator()
    {
        RuleFor(x => x.TagIds)
            .NotEmpty()
            .WithMessage("Upsert habit tag IDs are required");
    }
}
