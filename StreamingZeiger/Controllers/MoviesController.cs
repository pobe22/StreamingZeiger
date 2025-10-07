using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IStaticMovieRepository _repo;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public MoviesController(IStaticMovieRepository repo, AppDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _repo = repo;
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "Query", "Genre", "Service", "MinRating", "YearFrom", "YearTo", "Page", "PageSize" })]
        public async Task<IActionResult> Index([FromQuery] MovieFilterViewModel filter)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            // Filter aus Session laden/speichern
            if (string.IsNullOrEmpty(filter.Query) &&
                string.IsNullOrEmpty(filter.Genre) &&
                string.IsNullOrEmpty(filter.Service) &&
                !filter.MinRating.HasValue &&
                !filter.YearFrom.HasValue &&
                !filter.YearTo.HasValue)
            {
                // Aufgabe 2: Formulardaten persistent halten
                var savedFilter = HttpContext.Session.GetObjectFromJson<MovieFilterViewModel>("MovieFilter");
                if (savedFilter != null)
                {
                    filter = savedFilter;
                }
            }
            else
            {
                // Aufgabe 4: Mehrere Formulare gleichzeitig unterstützen
                HttpContext.Session.SetObjectAsJson("MovieFilter", filter);
            }
            var cacheKey = $"movies_{filter.Query}_{filter.Genre}_{filter.Service}_{filter.MinRating}_{filter.YearFrom}_{filter.YearTo}_{filter.Page}_{filter.PageSize}";

            List<Movie> movies = new List<Movie>();

            if (!_cache.TryGetValue(cacheKey, out MovieIndexViewModel? vm))
            {
                var moviesQuery = _context.Movies
                .Include(m => m.MediaGenres).ThenInclude(mg => mg.Genre)
                .AsNoTracking();

                 movies = moviesQuery.ToList();

                if (!string.IsNullOrWhiteSpace(filter.Genre))
                    movies = movies.Where(m => m.MediaGenres.Any(mg => mg.Genre.Name == filter.Genre)).ToList();

                if (!string.IsNullOrWhiteSpace(filter.Service))
                    movies = movies.Where(m => m.AvailabilityByService != null &&
                                               m.AvailabilityByService.ContainsKey(filter.Service) &&
                                               m.AvailabilityByService[filter.Service]).ToList();

                if (filter.MinRating.HasValue)
                    movies = movies.Where(m => m.Rating >= filter.MinRating.Value).ToList();

                if (!string.IsNullOrWhiteSpace(filter.Query))
                    movies = movies.Where(m =>
                        (m.Title?.Contains(filter.Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (m.OriginalTitle?.Contains(filter.Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (m.Cast?.Any(c => c.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ?? false))
                        .ToList();

                if (filter.YearFrom.HasValue)
                    movies = movies.Where(m => m.Year >= filter.YearFrom.Value).ToList();

                if (filter.YearTo.HasValue)
                    movies = movies.Where(m => m.Year <= filter.YearTo.Value).ToList();

                _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(5));
            }

            var pagedMovies = movies.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

                // Watchlist-Status pro Film
                var moviesVm = pagedMovies.Select(m => new MovieIndexItemViewModel
                {
                    Movie = m,
                    InWatchlist = userId != null && _context.WatchlistItems.Any(w => w.UserId == userId && w.MediaItemId == m.Id)
                }).ToList();

                vm = new MovieIndexViewModel
                {
                    Movies = moviesVm,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Total = movies.Count,
                    Genre = filter.Genre,
                    Service = filter.Service,
                    MinRating = filter.MinRating.HasValue ? (int?)filter.MinRating.Value : null,
                    YearFrom = filter.YearFrom,
                    YearTo = filter.YearTo,
                    Query = filter.Query
                };

                var services = movies.SelectMany(m => m.AvailabilityByService.Keys).Distinct().OrderBy(s => s).ToList();
                ViewBag.Services = new SelectList(services, filter.Service);
                var genres = _context.Genres.OrderBy(g => g.Name).ToList();
                ViewBag.Genres = new SelectList(genres, "Name", "Name", filter.Genre);

            return View(vm);
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

        [HttpPost]
        public IActionResult Search(MovieFilterViewModel filter)
        {
            HttpContext.Session.SetObjectAsJson("MovieFilter", filter);

            return RedirectToAction("Index", filter);
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
