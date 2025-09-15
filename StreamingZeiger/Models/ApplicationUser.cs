using Microsoft.AspNetCore.Identity;

namespace StreamingZeiger.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<WatchlistItem> Watchlist { get; set; }
    }
}
