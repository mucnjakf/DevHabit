namespace DevHabit.Api.Dtos.Common;

public interface ICollectionResponseDto<T>
{
    List<T> Items { get; init; }
}
