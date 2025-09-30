using StreamingZeiger.Models;

namespace StreamingZeiger.ViewModels
{
    public class WatchlistViewModel
    {
        public List<WatchlistItem> Movies { get; set; } = new();
        public List<WatchlistItem> Series { get; set; } = new();
    }
}
