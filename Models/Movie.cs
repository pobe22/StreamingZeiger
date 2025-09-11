using System.Collections.Generic;

namespace StreamingZeiger.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public List<string> Genres { get; set; } = new();
        public int Year { get; set; }
        public int DurationMinutes { get; set; }
        public string Description { get; set; }
        public List<string> Cast { get; set; } = new();
        public string Director { get; set; }
        public string PosterFile { get; set; } // path under wwwroot/images/posters
        public string TrailerUrl { get; set; }
        public Dictionary<string, bool> AvailabilityByService { get; set; } = new();
        public double Rating { get; set; } // average
    }
}
