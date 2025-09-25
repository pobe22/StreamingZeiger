using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamingZeiger.Models
{
    public class Series : MediaItem
    {
        public int StartYear { get; set; }
        public int? EndYear { get; set; }

        public ICollection<Season> Seasons { get; set; } = new List<Season>();
    }

    public class Season
    {
        public int Id { get; set; }
        public int SeasonNumber { get; set; }

        public int SeriesId { get; set; }
        public Series Series { get; set; }

        public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    }

    public class Episode
    {
        public int Id { get; set; }
        public int EpisodeNumber { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }

        public int SeasonId { get; set; }
        public Season Season { get; set; }
        public int DurationMinutes { get; set; }
    }

}
