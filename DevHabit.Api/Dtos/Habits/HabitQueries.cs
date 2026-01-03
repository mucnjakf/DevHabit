using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Habits;

public static class HabitQueries
{
    public static Expression<Func<Habit, HabitDto>> ProjectToDto()
    {
        return x => new HabitDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            Type = x.Type,
            Frequency = new FrequencyDto
            {
                Type = x.Frequency.Type,
                TimesPerPeriod = x.Frequency.TimesPerPeriod
            },
            Target = new TargetDto
            {
                Value = x.Target.Value,
                Unit = x.Target.Unit
            },
            Status = x.Status,
            IsArchived = x.IsArchived,
            EndDate = x.EndDate,
            Milestone = x.Milestone == null
                ? null
                : new MilestoneDto
                {
                    Target = x.Milestone.Target,
                    Current = x.Milestone.Current
                },
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
            LastCompletedAtUtc = x.LastCompletedAtUtc
        };
    }
}
