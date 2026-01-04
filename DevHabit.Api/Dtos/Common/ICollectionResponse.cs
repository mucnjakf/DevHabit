namespace DevHabit.Api.Dtos.Common;

public interface ICollectionResponse<T>
{
    List<T> Items { get; init; }
}
