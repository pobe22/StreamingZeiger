using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace StreamingZeiger.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public int Year { get; set; }
        public int DurationMinutes { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Cast { get; set; } = new(); 
        public string Director { get; set; } = string.Empty;
        public string PosterFile { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        public Dictionary<string, bool> AvailabilityByService { get; set; } = new(); 
        public double Rating { get; set; }

        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
        [ValidateNever]
        public ICollection<Rating> Ratings { get; set; }
        [ValidateNever]
        public ICollection<WatchlistItem> WatchlistItems { get; set; }

    }

    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    }

    // Join-Tabelle für n:m
    public class MovieGenre
    {
        public int MovieId { get; set; }
        public Movie Movie { get; set; } = null!;

        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
