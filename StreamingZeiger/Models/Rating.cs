namespace StreamingZeiger.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int MediaItemId { get; set; }
        public MediaItem MediaItem { get; set; }
    }
}
