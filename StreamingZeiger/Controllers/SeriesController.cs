using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Controllers
{
    public class SeriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public SeriesController(AppDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // ======================
        // SERIES
        // ======================

        // Liste aller Serien
        [Authorize]
        public async Task<IActionResult> Index([FromQuery] MediaFilterViewModel filter)
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
                var savedFilter = HttpContext.Session.GetObjectFromJson<MediaFilterViewModel>("SeriesFilter");
                if (savedFilter != null)
                    filter = savedFilter;
                else
                    filter = new MediaFilterViewModel();
            }
            else
            {
                HttpContext.Session.SetObjectAsJson("SeriesFilter", filter);
            }

            var cacheKey = $"series_{filter.Query}_{filter.Genre}_{filter.Service}_{filter.MinRating}_{filter.YearFrom}_{filter.YearTo}_{filter.Page}_{filter.PageSize}";

            List<Series> series = new List<Series>();

            if (!_cache.TryGetValue(cacheKey, out MediaIndexViewModel? vm))
            {
                var seriesQuery = _context.Series
                    .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                    .AsNoTracking();

                series = seriesQuery.ToList();

                if (!string.IsNullOrWhiteSpace(filter.Genre))
                    series = series.Where(s => s.MediaGenres.Any(mg => mg.Genre.Name == filter.Genre)).ToList();

                if (!string.IsNullOrWhiteSpace(filter.Service))
                    series = series.Where(s => s.AvailabilityByService != null &&
                                               s.AvailabilityByService.ContainsKey(filter.Service) &&
                                               s.AvailabilityByService[filter.Service]).ToList();

                if (filter.MinRating.HasValue)
                    series = series.Where(s => s.Rating >= filter.MinRating.Value).ToList();

                if (!string.IsNullOrWhiteSpace(filter.Query))
                    series = series.Where(s =>
                        (s.Title?.Contains(filter.Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.OriginalTitle?.Contains(filter.Query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.Cast?.Any(c => c.Contains(filter.Query, StringComparison.OrdinalIgnoreCase)) ?? false))
                        .ToList();

                if (filter.YearFrom.HasValue)
                    series = series.Where(s => s.StartYear >= filter.YearFrom.Value).ToList();

                if (filter.YearTo.HasValue)
                    series = series.Where(s => s.EndYear <= filter.YearTo.Value).ToList();

                var pagedSeries = series.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

                var seriesVm = pagedSeries.Select(s => new MediaItemViewModel
                {
                    Series = s,
                    Title = s.Title,
                    InWatchlist = userId != null && _context.WatchlistItems.Any(w => w.UserId == userId && w.MediaItemId == s.Id)
                }).ToList();

                vm = new MediaIndexViewModel
                {
                    Items = seriesVm,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    Total = series.Count,
                    Genre = filter.Genre,
                    Service = filter.Service,
                    MinRating = filter.MinRating.HasValue ? (int?)filter.MinRating.Value : null,
                    YearFrom = filter.YearFrom,
                    YearTo = filter.YearTo,
                    Query = filter.Query
                };

                _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(5));
            }
            else
            {
                series = vm.Items.Select(s => s.Series).ToList();
            }

            var services = _context.Series
                .AsNoTracking()
                .Where(s => s.AvailabilityByService != null)
                .AsEnumerable()
                .SelectMany(s => s.AvailabilityByService.Keys)
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


        // Details: Serie mit Seasons und Episodes
        public async Task<IActionResult> Details(int id)
        {
            var series = await _context.Series
                .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                .Include(s => s.Seasons)
                    .ThenInclude(se => se.Episodes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            // Empfehlungen
            var genreIds = series.MediaGenres.Select(mg => mg.Genre.Id).ToList();
            var recommended = await _context.Series
                .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                .Where(s => s.Id != id && s.MediaGenres.Any(mg => genreIds.Contains(mg.Genre.Id)))
                .OrderByDescending(s => s.Rating)
                .Take(6)
                .ToListAsync();

            ViewBag.RecommendedSeries = recommended;

            // Watchlist prüfen
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

            return View(series);
        }

        // ======================
        // SEASONS
        // ======================

        public async Task<IActionResult> SeasonDetails(int id)
        {
            var season = await _context.Seasons
                .Include(se => se.Episodes)
                .Include(se => se.Series)
                .FirstOrDefaultAsync(se => se.Id == id);

            if (season == null) return NotFound();
            return View(season);
        }

        // ======================
        // EPISODES
        // ======================

        public async Task<IActionResult> EpisodeDetails(int id)
        {
            var episode = await _context.Episodes
                .Include(e => e.Season).ThenInclude(s => s.Series)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (episode == null) return NotFound();
            return View(episode);
        }

    }
}
