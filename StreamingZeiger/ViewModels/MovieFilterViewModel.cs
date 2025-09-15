namespace StreamingZeiger.ViewModels;
public class MovieFilterViewModel
{
    public string? Query { get; set; }
    public string? Genre { get; set; }
    public int? YearFrom { get; set; }
    public int? YearTo { get; set; }
    public double? MinRating { get; set; }
    public string? Service { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

