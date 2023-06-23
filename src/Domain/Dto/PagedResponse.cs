namespace Domain.Dto;

public class PagedResponse<T>
{
    public IList<T> Data { get; set; } = default!;
    public string? NextToken { get; set; }
    public int Limit { get; set; }
    public string? PreviousToken { get; set; }
}