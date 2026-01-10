using DevHabit.Api.Entities;
using DevHabit.Api.Enums;
using DevHabit.Api.Services.Sorting;

namespace DevHabit.Api.Dtos.Habits;

public static class HabitMappings
{
    public static readonly SortMappingDefinition<HabitDto, Habit> SortMapping = new()
    {
        Mapping =
        [
            new SortMapping(nameof(HabitDto.Name), nameof(Habit.Name)),
            new SortMapping(nameof(HabitDto.Description), nameof(Habit.Description)),
            new SortMapping(nameof(HabitDto.Type), nameof(Habit.Type)),

            new SortMapping(
                $"{nameof(HabitDto.Frequency)}.{nameof(HabitDto.Frequency.Type)}",
                $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.Type)}"),

            new SortMapping(
                $"{nameof(HabitDto.Frequency)}.{nameof(HabitDto.Frequency.TimesPerPeriod)}",
                $"{nameof(Habit.Frequency)}.{nameof(Habit.Frequency.TimesPerPeriod)}"),

            new SortMapping(
                $"{nameof(HabitDto.Target)}.{nameof(HabitDto.Target.Value)}",
                $"{nameof(Habit.Target)}.{nameof(Habit.Target.Value)}"),

            new SortMapping(
                $"{nameof(HabitDto.Target)}.{nameof(HabitDto.Target.Unit)}",
                $"{nameof(Habit.Target)}.{nameof(Habit.Target.Unit)}"),

            new SortMapping(nameof(HabitDto.Status), nameof(Habit.Status)),
            new SortMapping(nameof(HabitDto.EndDate), nameof(Habit.EndDate)),
            new SortMapping(nameof(HabitDto.CreatedAtUtc), nameof(Habit.CreatedAtUtc)),
            new SortMapping(nameof(HabitDto.UpdatedAtUtc), nameof(Habit.UpdatedAtUtc)),
            new SortMapping(nameof(HabitDto.LastCompletedAtUtc), nameof(Habit.LastCompletedAtUtc)),
        ]
    };

    public static HabitDto ToDto(this Habit habit)
    {
        return new HabitDto
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            Type = habit.Type,
            Frequency = new FrequencyDto
            {
                Type = habit.Frequency.Type,
                TimesPerPeriod = habit.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = habit.Target.Value,
                Unit = habit.Target.Unit
            },
            Status = habit.Status,
            IsArchived = habit.IsArchived,
            EndDate = habit.EndDate,
            Milestone = habit.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = habit.Milestone.Target,
                    Current = habit.Milestone.Current
                },
            CreatedAtUtc = habit.CreatedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc,
            LastCompletedAtUtc = habit.LastCompletedAtUtc
        };
    }

    public static Habit ToEntity(this CreateHabitRequest request, string userId)
    {
        return new Habit
        {
            Id = $"h_{Guid.CreateVersion7()}",
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Frequency = new Frequency
            {
                Type = request.Frequency.Type,
                TimesPerPeriod = request.Frequency.TimesPerPeriod
            },
            Target = new Target
            {
                Value = request.Target.Value,
                Unit = request.Target.Unit
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = request.EndDate,
            Milestone = request.Milestone == null
                ? null
                : new Milestone
                {
                    Target = request.Milestone.Target,
                    Current = request.Milestone.Current
                },
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void UpdateFromRequest(this Habit habit, UpdateHabitRequest request)
    {
        habit.Name = request.Name;
        habit.Description = request.Description;
        habit.Type = request.Type;
        habit.EndDate = request.EndDate;

        habit.Frequency = new Frequency
        {
            Type = request.Frequency.Type,
            TimesPerPeriod = request.Frequency.TimesPerPeriod
        };

        habit.Target = new Target
        {
            Value = request.Target.Value,
            Unit = request.Target.Unit
        };

        if (request.Milestone != null)
        {
            habit.Milestone ??= new Milestone();
            habit.Milestone.Target = request.Milestone.Target;
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}
