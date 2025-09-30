using StreamingZeiger.Models;

namespace StreamingZeiger.ViewModels
{
    public class MovieIndexItemViewModel
    {
        public Movie Movie { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool InWatchlist { get; set; }
    }

}
