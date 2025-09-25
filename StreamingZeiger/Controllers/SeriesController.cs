using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
    public class SeriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SeriesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ======================
        // SERIES
        // ======================

        // Liste aller Serien
        public async Task<IActionResult> Index()
        {
            var series = await _context.Series
                .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                .AsNoTracking()
                .ToListAsync();

            return View(series);
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

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Series series)
        {
            if (id != series.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(series);
        }

        // Delete
        public async Task<IActionResult> Delete(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series == null) return NotFound();
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
