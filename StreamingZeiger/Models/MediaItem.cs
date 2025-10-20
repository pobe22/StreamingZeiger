using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamingZeiger.Models
{
    public abstract class MediaItem
    {
        public int Id { get; set; }
        public int? TmdbId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Cast { get; set; } = new();
        public string Director { get; set; } = string.Empty;
        public string PosterFile { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        public Dictionary<string, bool> AvailabilityByService { get; set; } = new();
        public double Rating { get; set; }

        [ValidateNever]
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

        [ValidateNever]
        public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();

        public ICollection<MediaGenre> MediaGenres { get; set; } = new List<MediaGenre>();
        [NotMapped]
        public IEnumerable<Genre> Genres => MediaGenres.Select(mg => mg.Genre);
    }
}
