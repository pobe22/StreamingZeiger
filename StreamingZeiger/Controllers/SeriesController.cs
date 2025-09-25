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

        // Create
        public IActionResult Create()
        {
            ViewBag.Genres = new SelectList(_context.Genres, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Series series, int[] genreIds)
        {
            if (ModelState.IsValid)
            {
                foreach (var genreId in genreIds)
                {
                    series.MediaGenres.Add(new MediaGenre { GenreId = genreId });
                }
                _context.Series.Add(series);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(series);
        }

        // Edit
        public async Task<IActionResult> Edit(int id)
        {
            var series = await _context.Series
                .Include(s => s.MediaGenres)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (series == null) return NotFound();

            ViewBag.Genres = new SelectList(_context.Genres, "Id", "Name");
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

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series != null)
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
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

        public IActionResult AddSeason(int seriesId)
        {
            return View(new Season { SeriesId = seriesId });
        }

        [HttpPost]
        public async Task<IActionResult> AddSeason(Season season)
        {
            if (ModelState.IsValid)
            {
                _context.Seasons.Add(season);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = season.SeriesId });
            }
            return View(season);
        }

        public async Task<IActionResult> EditSeason(int id)
        {
            var season = await _context.Seasons.FindAsync(id);
            if (season == null) return NotFound();
            return View(season);
        }

        [HttpPost]
        public async Task<IActionResult> EditSeason(int id, Season season)
        {
            if (id != season.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(season);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = season.SeriesId });
            }
            return View(season);
        }

        public async Task<IActionResult> DeleteSeason(int id)
        {
            var season = await _context.Seasons.Include(se => se.Series).FirstOrDefaultAsync(se => se.Id == id);
            if (season == null) return NotFound();
            return View(season);
        }

        [HttpPost, ActionName("DeleteSeason")]
        public async Task<IActionResult> DeleteSeasonConfirmed(int id)
        {
            var season = await _context.Seasons.FindAsync(id);
            if (season != null)
            {
                int seriesId = season.SeriesId;
                _context.Seasons.Remove(season);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = seriesId });
            }
            return NotFound();
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

        public IActionResult AddEpisode(int seasonId)
        {
            return View(new Episode { SeasonId = seasonId });
        }

        [HttpPost]
        public async Task<IActionResult> AddEpisode(Episode episode)
        {
            if (ModelState.IsValid)
            {
                _context.Episodes.Add(episode);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(SeasonDetails), new { id = episode.SeasonId });
            }
            return View(episode);
        }

        public async Task<IActionResult> EditEpisode(int id)
        {
            var episode = await _context.Episodes.FindAsync(id);
            if (episode == null) return NotFound();
            return View(episode);
        }

        [HttpPost]
        public async Task<IActionResult> EditEpisode(int id, Episode episode)
        {
            if (id != episode.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(episode);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(SeasonDetails), new { id = episode.SeasonId });
            }
            return View(episode);
        }

        public async Task<IActionResult> DeleteEpisode(int id)
        {
            var episode = await _context.Episodes.Include(e => e.Season).FirstOrDefaultAsync(e => e.Id == id);
            if (episode == null) return NotFound();
            return View(episode);
        }

        [HttpPost, ActionName("DeleteEpisode")]
        public async Task<IActionResult> DeleteEpisodeConfirmed(int id)
        {
            var episode = await _context.Episodes.FindAsync(id);
            if (episode != null)
            {
                int seasonId = episode.SeasonId;
                _context.Episodes.Remove(episode);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(SeasonDetails), new { id = seasonId });
            }
            return NotFound();
        }
    }
}
