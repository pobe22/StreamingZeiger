using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;
using StreamingZeiger.Services;

namespace StreamingZeiger.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IStaticMovieRepository _repo;
        public MoviesController(IStaticMovieRepository repo) { _repo = repo; }

        public IActionResult Index([FromQuery] MovieFilterViewModel filter)
        {
            var movies = _repo.GetAll();
            if (movies == null)
                return BadRequest("Repository did not return a valid movie list.");

            var q = movies.AsQueryable();
            if (!string.IsNullOrWhiteSpace(filter.Query))
                q = q.Where(m => m.Title.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)
                              || m.OriginalTitle.Contains(filter.Query ?? "", StringComparison.OrdinalIgnoreCase)
                              || m.Cast.Any(c => c.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)));
            if (!string.IsNullOrWhiteSpace(filter.Genre))
                q = q.Where(m => m.Genres.Contains(filter.Genre));
            if (filter.YearFrom.HasValue) q = q.Where(m => m.Year >= filter.YearFrom.Value);
            if (filter.YearTo.HasValue) q = q.Where(m => m.Year <= filter.YearTo.Value);
            if (filter.MinRating.HasValue) q = q.Where(m => m.Rating >= filter.MinRating.Value);
            if (!string.IsNullOrWhiteSpace(filter.Service))
                q = q.Where(m => m.AvailabilityByService.ContainsKey(filter.Service) && m.AvailabilityByService[filter.Service]);

            var total = q.Count();
            var items = q.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            ViewBag.Total = total;
            ViewBag.Page = filter.Page;
            ViewBag.PageSize = filter.PageSize;

            return View(items);
        }

        public IActionResult Details(int id)
        {
            var movie = _repo.GetById(id);
            if (movie == null) return NotFound();
            return View(movie);
        }
    }
}
