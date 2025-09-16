using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamingZeiger.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
        public int Seasons { get; set; }
        public int Episodes { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Cast { get; set; } = new();
        public string Director { get; set; } = string.Empty;
        public string PosterFile { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        [NotMapped]
        public Dictionary<string, bool> AvailabilityByService { get; set; } = new();
        public double Rating { get; set; }

        public ICollection<SeriesGenre> SeriesGenres { get; set; } = new List<SeriesGenre>();
        [ValidateNever]
        public ICollection<Rating> Ratings { get; set; }
        [ValidateNever]
        public ICollection<WatchlistItem> WatchlistItems { get; set; }
    }

    public class SeriesGenre
    {
        public int SeriesId { get; set; }
        public Series Series { get; set; } = null!;

        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
