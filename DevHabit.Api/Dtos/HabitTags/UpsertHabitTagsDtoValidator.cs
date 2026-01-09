using FluentValidation;

namespace DevHabit.Api.Dtos.HabitTags;

public sealed class UpsertHabitTagsDtoValidator : AbstractValidator<UpsertHabitTagsDto>
{
    public UpsertHabitTagsDtoValidator()
    {
        RuleFor(x => x.TagIds)
            .NotEmpty()
            .WithMessage("Upsert habit tag IDs are required");
    }
}
