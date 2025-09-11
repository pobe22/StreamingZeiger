using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StreamingZeiger.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public int Year { get; set; }
        public int DurationMinutes { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Cast { get; set; } = new();
        public string Director { get; set; } = string.Empty;
        public string PosterFile { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        public Dictionary<string, bool> AvailabilityByService { get; set; } = new();
        public double Rating { get; set; }
    }
}
