namespace StreamingZeiger.Models
{
    public class Rating
    {
        public int Id { get; set; }
        public int Score { get; set; } // z.B. 1-5 Sterne
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public int MovieId { get; set; }
        public Movie Movie { get; set; }
    }
}
