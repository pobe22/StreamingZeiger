using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Übersicht
        public async Task<IActionResult> Index()
        {
            var vm = new AdminIndexViewModel
            {
                Movies = await _context.Movies
                    .Include(m => m.MediaGenres)
                        .ThenInclude(mg => mg.Genre)
                    .ToListAsync(),

                Series = await _context.Series
                    .Include(s => s.MediaGenres)
                        .ThenInclude(mg => mg.Genre)
                    .ToListAsync()
            };

            return View(vm);
        }


        // ---------- Gemeinsamer Create-Helper ----------
        private async Task HandleMediaItemBaseAsync(MediaItem item, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            // Cast
            if (!string.IsNullOrWhiteSpace(castCsv))
                item.Cast = castCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(c => c.Trim())
                                   .ToList();

            // Dienste
            item.AvailabilityByService = services?.ToDictionary(s => s, s => true) ?? new();

            // Poster
            if (posterUpload != null && posterUpload.Length > 0)
            {
                var extension = Path.GetExtension(posterUpload.FileName);
                var fileName = Path.GetFileNameWithoutExtension(posterUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", uniqueFileName);

                using var stream = new FileStream(savePath, FileMode.Create);
                await posterUpload.CopyToAsync(stream);

                item.PosterFile = "/images/posters/" + uniqueFileName;
            }

            // Genres
            if (!string.IsNullOrWhiteSpace(genreCsv))
            {
                var genreNames = genreCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var name in genreNames)
                {
                    // Prüfen, ob Genre schon existiert
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

                    if (genre == null)
                    {
                        genre = new Genre { Name = name };
                        _context.Genres.Add(genre);
                    }

                    // MediaGenre korrekt hinzufügen
                    if (!item.MediaGenres.Any(mg => mg.Genre.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    {
                        item.MediaGenres.Add(new MediaGenre
                        {
                            MediaItem = item,  
                            Genre = genre      
                        });
                    }
                }
            }

        }

        // ---------- Movies ----------
        public IActionResult CreateMovie() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(Movie movie, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            if (!ModelState.IsValid) return View(movie);

            await HandleMediaItemBaseAsync(movie, castCsv, services, genreCsv, posterUpload);

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Film erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditMovie(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null) return NotFound();

            ViewBag.CastCsv = string.Join(", ", movie.Cast ?? new List<string>());
            ViewBag.GenreCsv = string.Join(", ", movie.MediaGenres.Select(mg => mg.Genre.Name));

            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(int id, Movie movie, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            if (id != movie.Id) return NotFound();
            if (!ModelState.IsValid) return View(movie);

            var existing = await _context.Movies
                .Include(m => m.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (existing == null) return NotFound();

            _context.Entry(existing).CurrentValues.SetValues(movie);
            existing.MediaGenres.Clear();

            await HandleMediaItemBaseAsync(existing, castCsv, services, genreCsv, posterUpload);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Film erfolgreich bearbeitet.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteMovie(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            _context.Movies.Remove(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // ---------- Series ----------
        public IActionResult CreateSeries() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeries(Series series, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            if (!ModelState.IsValid) return View(series);

            await HandleMediaItemBaseAsync(series, castCsv, services, genreCsv, posterUpload);

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (series == null) return NotFound();

            ViewBag.CastCsv = string.Join(", ", series.Cast ?? new List<string>());
            ViewBag.GenreCsv = string.Join(", ", series.MediaGenres.Select(mg => mg.Genre.Name));

            return View(series);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeries(int id, Series series, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            if (id != series.Id) return NotFound();
            if (!ModelState.IsValid) return View(series);

            var existing = await _context.Series
                .Include(s => s.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            _context.Entry(existing).CurrentValues.SetValues(series);
            existing.MediaGenres.Clear();

            await HandleMediaItemBaseAsync(existing, castCsv, services, genreCsv, posterUpload);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich bearbeitet.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteSeries(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series == null) return NotFound();

            _context.Series.Remove(series);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ImportFromTmdb(int tmdbId, string type)
        {
            var tmdbService = new TmdbService();

            if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
            {
                var movie = await tmdbService.GetMovieByIdAsync(tmdbId);
                if (movie == null)
                    return NotFound(new { message = "Film nicht gefunden" });

                return Json(new
                {
                    title = movie.Title,
                    originalTitle = movie.OriginalTitle,
                    year = movie.Year,
                    durationMinutes = movie.DurationMinutes,
                    description = movie.Description,
                    director = movie.Director,
                    posterFile = movie.PosterFile,
                    trailerUrl = movie.TrailerUrl,
                    cast = movie.Cast,
                    genres = movie.MediaGenres?.Select(mg => mg.Genre.Name).ToList()
                });
            }
            else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
            {
                var series = await tmdbService.GetSeriesByIdAsync(tmdbId);
                if (series == null)
                    return NotFound(new { message = "Serie nicht gefunden" });

                return Json(new
                {
                    title = series.Title,
                    originalTitle = series.OriginalTitle,
                    startYear = series.StartYear,
                    endYear = series.EndYear,
                    seasons = series.Seasons,
                    episodes = series.Episodes,
                    description = series.Description,
                    director = series.Director,
                    posterFile = series.PosterFile,
                    trailerUrl = series.TrailerUrl,
                    cast = series.Cast,
                    genres = series.MediaGenres?.Select(mg => mg.Genre.Name).ToList()
                });
            }

            return BadRequest(new { message = "Ungültiger Typ. Erlaubt: 'movie' oder 'series'." });
        }

    }
}
