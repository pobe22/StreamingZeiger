using StreamingZeiger.Data;
using StreamingZeiger.Models;

namespace StreamingZeiger.Services
{
    public class LoggingService
    {
        private readonly AppDbContext _context;

        public LoggingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string title, string description)
        {
            var entry = new LogEntry { Title = title, Description = description };
            _context.LogEntries.Add(entry);
            await _context.SaveChangesAsync();
        }
    }
}
