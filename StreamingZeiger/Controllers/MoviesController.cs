using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IStaticMovieRepository _repo;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MoviesController(IStaticMovieRepository repo, AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _repo = repo;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index([FromQuery] MovieFilterViewModel filter)
        {
            var movies = _context.Movies
     .Include(m => (m as MediaItem).MediaGenres)
         .ThenInclude(mg => mg.Genre)
     .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Genre))
            {
                movies = movies.Where(m => m.MediaGenres.Any(mg => mg.Genre.Name == filter.Genre));
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

        public async Task<IActionResult> Details(int id)
        {
            var movie = await _context.Movies
                .Include(m => (m as MediaItem).MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            // Empfehlungen: gleiche Genres, außer aktueller Film
            var genreIds = movie.MediaGenres.Select(mg => mg.Genre.Id).ToList();

            var recommended = await _context.Movies
                .Include(m => (m as MediaItem).MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .Where(m => m.Id != id && m.MediaGenres.Any(mg => genreIds.Contains(mg.Genre.Id)))
                .OrderByDescending(m => m.Rating)
                .Take(8)
                .ToListAsync();

            ViewBag.RecommendedMovies = recommended;

            // Prüfen, ob aktueller User den Film in der Watchlist hat
            bool inWatchlist = false;

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    inWatchlist = await _context.WatchlistItems
                        .AnyAsync(w => w.UserId == user.Id && w.MediaItemId == id);
                }
            }
            ViewBag.InWatchlist = inWatchlist;

            return View(movie);
        }

        [HttpGet]
        public JsonResult Autocomplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new string[0]);

            var titles = _context.Movies
                .Where(m => m.Title.Contains(term))
                .OrderBy(m => m.Title)
                .Select(m => m.Title)
                .Take(10)
                .ToList();

            return Json(titles);
        }

        public IActionResult TrackShare(int id, string platform)
        {
            var movie = _context.Movies.Find(id);
            if (movie != null)
            {
                // movie.ShareCount++; // optional
                _context.SaveChanges();
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
