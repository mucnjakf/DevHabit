namespace DevHabit.Api.Dtos.Common;

public sealed record PaginationDto<T> : ICollectionResponseDto<T>, ILinksResponseDto
{
    public List<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public List<LinkDto> Links { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
