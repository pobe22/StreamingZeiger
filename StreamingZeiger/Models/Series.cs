namespace StreamingZeiger.Models
{
    public class Series
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Seasons { get; set; }
        public int Episodes { get; set; }
        public string PosterUrl { get; set; } = string.Empty;
    }
}
