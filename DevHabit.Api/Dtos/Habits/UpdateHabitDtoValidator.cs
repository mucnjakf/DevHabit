using DevHabit.Api.Entities;
using FluentValidation;

namespace DevHabit.Api.Dtos.Habits;

public sealed class UpdateHabitDtoValidator : AbstractValidator<UpdateHabitDto>
{
    private static readonly string[] AllowedUnits =
        ["minutes", "hours", "steps", "km", "cal", "pages", "books", "tasks", "sessions"];

    private static readonly string[] AllowedUnitsForBinaryHabits = ["sessions", "tasks"];

    public UpdateHabitDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100)
            .WithMessage("Habit name must be between 3 and 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null)
            .WithMessage("Habit description cannot exceed 500 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Habit type is invalid");

        RuleFor(x => x.Frequency.Type)
            .IsInEnum()
            .WithMessage("Habit frequency type is invalid");

        RuleFor(x => x.Frequency.TimesPerPeriod)
            .GreaterThan(0)
            .WithMessage("Habit frequency times per period must be greater than 0");

        RuleFor(x => x.Target.Value)
            .GreaterThan(0)
            .WithMessage("Habit target value must be greater than 0");

        RuleFor(x => x.Target.Unit)
            .NotEmpty()
            .Must(x => AllowedUnits.Contains(x.ToLowerInvariant()))
            .WithMessage($"Habit target unit must be one of: {string.Join(", ", AllowedUnits)}");

        RuleFor(x => x.Target.Unit)
            .Must((dto, unit) => IsTargetUnitCompatibleWithType(dto.Type, unit))
            .WithMessage("Habit target unit is not compatible with the habit type");

        RuleFor(x => x.EndDate)
            .Must(x => x is null || x.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Habit end date must be in the future");

        When(x => x.Milestone is not null, () =>
        {
            RuleFor(x => x.Milestone!.Target)
                .GreaterThan(0)
                .WithMessage("Habit milestone target must be greater than 0");
        });
    }

    private static bool IsTargetUnitCompatibleWithType(HabitType type, string unit)
    {
        string normalizedUnit = unit.ToLowerInvariant();

        return type switch
        {
            HabitType.Binary => AllowedUnitsForBinaryHabits.Contains(normalizedUnit),
            HabitType.Measurable => AllowedUnits.Contains(normalizedUnit),
            _ => false
        };
    }
}
