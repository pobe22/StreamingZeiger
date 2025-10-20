using System;

namespace StreamingZeiger.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
