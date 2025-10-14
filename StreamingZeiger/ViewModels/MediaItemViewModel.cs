using StreamingZeiger.Models;

namespace StreamingZeiger.ViewModels
{
    public class MediaItemViewModel
    {
        public Movie Movie { get; set; }
        public Series Series { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool InWatchlist { get; set; }
        public string MediaType => Movie != null ? "Movie" : "Series";
    }

}
