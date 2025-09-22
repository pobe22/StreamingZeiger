namespace StreamingZeiger.Models
{
    public class WatchlistItem
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }

        public int? SeriesId { get; set; }
        public Series? Series { get; set; }
    }
}
