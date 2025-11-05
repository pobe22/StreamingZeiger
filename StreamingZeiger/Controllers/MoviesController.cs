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

        //[OutputCache(Duration = 60, VaryByQueryKeys = new[] { "Query", "Genre", "Service", "MinRating", "YearFrom", "YearTo", "Page", "PageSize" })]
        public async Task<IActionResult> Index([FromQuery] MediaFilterViewModel filter)
        {
            if (ModelState.IsValid == false)
            {
                filter = new MediaFilterViewModel();
            }
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            // --- 1. Filter aus Session laden (ohne Page überschreiben) ---
            var savedFilter = HttpContext.Session.GetObjectFromJson<MediaFilterViewModel>("MovieFilter");
            if (savedFilter != null &&
                string.IsNullOrEmpty(filter.Query) &&
                string.IsNullOrEmpty(filter.Genre) &&
                string.IsNullOrEmpty(filter.Service) &&
                !filter.MinRating.HasValue &&
                !filter.YearFrom.HasValue &&
                !filter.YearTo.HasValue)
            {
                var tempPage = filter.Page;
                filter = savedFilter;
                filter.Page = tempPage <= 0 ? 1 : tempPage;
            }

            // Filter aus Formular speichern, falls gesetzt
            if (!string.IsNullOrEmpty(filter.Query) ||
                !string.IsNullOrEmpty(filter.Genre) ||
                !string.IsNullOrEmpty(filter.Service) ||
                filter.MinRating.HasValue ||
                filter.YearFrom.HasValue ||
                filter.YearTo.HasValue)
            {
                HttpContext.Session.SetObjectAsJson("MovieFilter", filter);
            }

            // --- 2. Default-Werte für Paging ---
            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            // --- 3. Cache-Key ---
            var cacheKey = $"movies_{userId}_{filter.Query}_{filter.Genre}_{filter.Service}_{filter.MinRating}_{filter.YearFrom}_{filter.YearTo}_{filter.Page}_{filter.PageSize}";
            MediaIndexViewModel? vm = null;

            if (userId == null && _cache.TryGetValue(cacheKey, out vm))
            {
                // Cache verwenden
            }
            else
            {
                // --- 4. Query erstellen ---
                var moviesQuery = _context.Movies
                    .Include(m => m.MediaGenres).ThenInclude(mg => mg.Genre)
                    .AsNoTracking()
                    .AsQueryable();

                // Filter anwenden
                if (!string.IsNullOrEmpty(filter.Genre))
                    moviesQuery = moviesQuery.Where(m => m.MediaGenres.Any(mg => mg.Genre.Name == filter.Genre));

                if (!string.IsNullOrEmpty(filter.Service))
                    moviesQuery = moviesQuery.Where(m => m.AvailabilityByService != null &&
                                                         m.AvailabilityByService.ContainsKey(filter.Service) &&
                                                         m.AvailabilityByService[filter.Service]);

                if (filter.MinRating.HasValue)
                    moviesQuery = moviesQuery.Where(m => m.Rating >= filter.MinRating.Value);

                if (!string.IsNullOrEmpty(filter.Query))
                    moviesQuery = moviesQuery.Where(m =>
                        (m.Title != null && m.Title.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ||
                        (m.OriginalTitle != null && m.OriginalTitle.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ||
                        (m.Cast != null && m.Cast.Any(c => c.Contains(filter.Query, StringComparison.OrdinalIgnoreCase))));

                if (filter.YearFrom.HasValue)
                    moviesQuery = moviesQuery.Where(m => m.Year >= filter.YearFrom.Value);

                if (filter.YearTo.HasValue)
                    moviesQuery = moviesQuery.Where(m => m.Year <= filter.YearTo.Value);

                // --- 5. Total Count ---
                var total = await moviesQuery.CountAsync();

                // --- 6. Paging ---
                var pagedMovies = await moviesQuery
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // --- 7. ViewModel erstellen ---
                var moviesVm = pagedMovies.Select(m => new MediaItemViewModel
                {
                    Movie = m,
                    InWatchlist = userId != null && _context.WatchlistItems.Any(w => w.UserId == userId && w.MediaItemId == m.Id)
                }).ToList();

                vm = new MediaIndexViewModel
                {
                    Items = moviesVm,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Total = total,
                    Genre = filter.Genre,
                    Service = filter.Service,
                    MinRating = filter.MinRating,
                    YearFrom = filter.YearFrom,
                    YearTo = filter.YearTo,
                    Query = filter.Query
                };

                // --- 8. Cache setzen ---
                _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(5));
            }

            // --- 9. ViewBag für Filter ---
            var services = _context.Movies
                .AsNoTracking()
                .Where(m => m.AvailabilityByService != null)
                .AsEnumerable()
                .SelectMany(m => m.AvailabilityByService.Keys)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            ViewBag.Services = new SelectList(services, filter.Service);

            var genres = _context.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .AsEnumerable()
                .GroupBy(g => g.Name)
                .Select(g => g.First())
                .ToList();

            ViewBag.Genres = new SelectList(genres, "Name", "Name", filter.Genre);

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest();
            }
            var movie = await _context.Movies
                .Include(m => (m as MediaItem).MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            // Empfehlungen: gleiche Genres, außer aktueller Film
            var genreIds = movie.MediaGenres.Select(mg => mg.Genre.Id).ToList();

            var recommended = await _context.Movies
                .Include(m => m.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .Where(m => m.Id != id && m.MediaGenres.Any(mg => movie.MediaGenres.Select(x => x.GenreId).Contains(mg.GenreId)))
                .OrderByDescending(m => m.Rating)
                .Take(8)
                .ToListAsync();

            ViewBag.RecommendedMovies = recommended;


            bool inWatchlist = false;

            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                inWatchlist = false;
            }
            else
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

        public async Task<IActionResult> DetailsPartial(int id)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest();
            }
            var movie = await _context.Movies
                .Include(m => m.MediaGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            bool inWatchlist = false;
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {               
                inWatchlist = false;
            }
            else
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    inWatchlist = await _context.WatchlistItems
                        .AnyAsync(w => w.UserId == user.Id && w.MediaItemId == id);
                }
            }
            ViewBag.InWatchlist = inWatchlist;

            return PartialView("_MovieDetailsPartial", movie);
        }


        [HttpPost]
        public IActionResult Search(MediaFilterViewModel filter)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest();
            }
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
            if (ModelState.IsValid == false)
            {
                return BadRequest();
            }
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
