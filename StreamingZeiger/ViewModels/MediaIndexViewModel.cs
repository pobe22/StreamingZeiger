using StreamingZeiger.Models;
using System.Collections.Generic;

namespace StreamingZeiger.ViewModels
{
    public class MediaIndexViewModel
    {
        // Gefilterte Filme
        public IEnumerable<MediaItemViewModel> Items { get; set; } = new List<MediaItemViewModel>();

        // Filter
        public string? Query { get; set; }
        public string? Genre { get; set; }
        public string? Service { get; set; }
        public double? MinRating { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int Total { get; set; } = 0;

        public bool InWatchlist { get; set; }
    }
}
