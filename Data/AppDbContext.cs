using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Models;

namespace StreamingZeiger.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Series> Series { get; set; }
    }
}
