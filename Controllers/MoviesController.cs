using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Services;

namespace StreamingZeiger.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IStaticMovieRepository _repo;
        private readonly AppDbContext _context;
        public MoviesController(IStaticMovieRepository repo, AppDbContext context)
        {
            _repo = repo;
            _context = context;
        }

        public IActionResult Index([FromQuery] MovieFilterViewModel filter)
        {
            var movies = _context.Movies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Genre))
            {
                movies = movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .Where(m => m.MovieGenres.Any(mg => mg.Genre.Name == filter.Genre));
            }
            if (filter.YearFrom.HasValue) movies = movies.Where(m => m.Year >= filter.YearFrom.Value);
            if (filter.YearTo.HasValue) movies = movies.Where(m => m.Year <= filter.YearTo.Value);
            if (filter.MinRating.HasValue) movies = movies.Where(m => m.Rating >= filter.MinRating.Value);

            var result = movies.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Service))
            {
                result = result.Where(m => m.AvailabilityByService != null
                                         && m.AvailabilityByService.ContainsKey(filter.Service)
                                         && m.AvailabilityByService[filter.Service]);
            }

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                result = result.Where(m =>
                    (!string.IsNullOrWhiteSpace(m.Title) && m.Title.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(m.OriginalTitle) && m.OriginalTitle.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ||
                    (m.Cast != null && m.Cast.Any(c => c.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)))
                );
            }

            var total = result.Count();
            var items = result.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            ViewBag.Total = total;
            ViewBag.Page = filter.Page;
            ViewBag.PageSize = filter.PageSize;

            return View(items);

        }

        public IActionResult Details(int id)
        {
            var movie = _context.Movies.FirstOrDefault(m => m.Id == id);
            if (movie == null) return NotFound();
            return View(movie);
        }
    }
}
