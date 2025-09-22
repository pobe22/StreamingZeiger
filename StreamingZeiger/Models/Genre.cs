namespace StreamingZeiger.Models
{
    public class Genre
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<MediaGenre> MediaGenres { get; set; } = new List<MediaGenre>();
    }

    public class MediaGenre
    {
        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; } = null!;

        public int GenreId { get; set; }
        public Genre Genre { get; set; } = null!;
    }
}
