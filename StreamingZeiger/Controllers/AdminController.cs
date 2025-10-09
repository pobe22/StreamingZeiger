using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Controllers
{
    [Authorize(Roles = "Admin, Redakteur")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly DynamicDbContextFactory _contextFactory;

        public AdminController(AppDbContext context, IWebHostEnvironment env, DynamicDbContextFactory contextFactory)
        {
            _context = context;
            _env = env;
            _contextFactory = contextFactory;
        }

        // Übersicht
        public async Task<IActionResult> Index()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
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
            if (!ModelState.IsValid) return View("CreateMovie", movie);

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
            var seasons = series.Seasons.ToList();
            for (int i = 0; i < seasons.Count; i++)
            {
                ModelState.Remove($"Seasons[{i}].Series");

                var episodes = seasons[i].Episodes.ToList();
                for (int j = 0; j < episodes.Count; j++)
                {
                    ModelState.Remove($"Seasons[{i}].Episodes[{j}].Season");
                }
            }

            if (!ModelState.IsValid) return View(series);

            await HandleMediaItemBaseAsync(series, castCsv, services, genreCsv, posterUpload);

            // Seasons und Episodes manuell setzen, falls aus ViewModel vorhanden
            if (series.Seasons != null)
            {
                foreach (var season in series.Seasons)
                {
                    season.Series = series; // FK setzen
                    if (season.Episodes != null)
                    {
                        foreach (var ep in season.Episodes)
                        {
                            ep.Season = season; // FK setzen
                        }
                    }
                }
            }

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                .Include(s => s.Seasons).ThenInclude(se => se.Episodes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            ViewBag.CastCsv = string.Join(", ", series.Cast ?? new List<string>());
            ViewBag.GenreCsv = string.Join(", ", series.MediaGenres.Select(mg => mg.Genre.Name));

            return View(series);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeries(int id, Series series, string castCsv, List<string> services, string genreCsv)
        {
            if (id != series.Id) return NotFound();

            // Entferne verschachtelte ModelState-Fehler
            if (series.Seasons != null)
            {
                var seasons = series.Seasons.ToList();
                for (int i = 0; i < seasons.Count; i++)
                {
                    ModelState.Remove($"Seasons[{i}].Series");

                    var episodes = seasons[i].Episodes?.ToList() ?? new List<Episode>();
                    for (int j = 0; j < episodes.Count; j++)
                    {
                        ModelState.Remove($"Seasons[{i}].Episodes[{j}].Season");
                    }
                }
            }

            if (!ModelState.IsValid) return View(series);

            var existing = await _context.Series
                .Include(s => s.MediaGenres).ThenInclude(mg => mg.Genre)
                .Include(s => s.Seasons).ThenInclude(se => se.Episodes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existing == null) return NotFound();

            // Basis-Eigenschaften aktualisieren
            _context.Entry(existing).CurrentValues.SetValues(series);
            existing.MediaGenres.Clear();

            await HandleMediaItemBaseAsync(existing, castCsv, services, genreCsv, null);

            // Seasons & Episodes synchronisieren
            if (series.Seasons != null)
            {
                // Alte Seasons und deren Episodes löschen
                foreach (var existingSeason in existing.Seasons.ToList())
                {
                    _context.Episodes.RemoveRange(existingSeason.Episodes);
                    _context.Seasons.Remove(existingSeason);
                }
                await _context.SaveChangesAsync(); // Wichtig: Änderungen sofort speichern

                // Neue Seasons & Episodes hinzufügen
                foreach (var season in series.Seasons)
                {
                    season.Series = existing;

                    if (season.Episodes != null)
                    {
                        foreach (var ep in season.Episodes)
                        {
                            ep.Season = season;
                        }
                    }

                    _context.Seasons.Add(season);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Serie erfolgreich bearbeitet.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.Seasons).ThenInclude(se => se.Episodes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            _context.Series.Remove(series); 
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ImportFromTmdb(int tmdbId, string type, string region = "DE")
        {
            try
            {
                var tmdbService = new TmdbService();

                if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
                {
                    var movie = await tmdbService.GetMovieByIdAsync(tmdbId, region);
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
                        genres = movie.MediaGenres?.Select(mg => mg.Genre.Name).ToList(),
                        services = movie.AvailabilityByService
                                   .Where(kv => kv.Value)
                                   .Select(kv => kv.Key)
                                   .ToList()
                    });
                }
                else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                {
                    var series = await tmdbService.GetSeriesByIdAsync(tmdbId, region);
                    if (series == null)
                        return NotFound(new { message = "Serie nicht gefunden" });

                    return Json(new
                    {
                        title = series.Title,
                        originalTitle = series.OriginalTitle,
                        startYear = series.StartYear,
                        endYear = series.EndYear,
                        seasons = series.Seasons?.Select(s => new
                        {
                            seasonNumber = s.SeasonNumber,
                            episodes = s.Episodes.Select(e => new
                            {
                                episodeNumber = e.EpisodeNumber,
                                title = e.Title,
                                description = e.Description,
                                durationMinutes = e.DurationMinutes
                            }).ToList()
                        }).ToList(),
                        description = series.Description,
                        director = series.Director,
                        posterFile = series.PosterFile,
                        trailerUrl = series.TrailerUrl,
                        cast = series.Cast,
                        genres = series.MediaGenres?.Select(mg => mg.Genre.Name).ToList(),
                        services = series.AvailabilityByService
                                   .Where(kv => kv.Value)
                                   .Select(kv => kv.Key)
                                   .ToList()
                    });
                }
                else
                {
                    return BadRequest(new { message = "Ungültiger Typ. 'movie' oder 'series' erwartet." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportMultiple(string tmdbIds, string type, string region = "DE")
        {
            if (string.IsNullOrWhiteSpace(tmdbIds))
            {
                TempData["Message"] = "Keine IDs angegeben.";
                return RedirectToAction(nameof(Index));
            }

            // IDs in Liste umwandeln
            var ids = tmdbIds
                 .Split(',', StringSplitOptions.RemoveEmptyEntries)
                 .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
                 .Where(id => id > 0)
                 .ToList();

            if (!ids.Any())
            {
                TempData["Message"] = "Keine gültigen IDs gefunden.";
                return RedirectToAction(nameof(Index));
            }


            var tmdbService = new TmdbService();

            foreach (var tmdbId in ids) 
                try
                {
                    if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
                    {
                        var movie = await tmdbService.GetMovieByIdAsync(tmdbId, region);
                        if (movie == null)
                        {
                            TempData["Message"] += $"Film-ID {tmdbId} nicht gefunden.\n";
                            continue;
                        }
                        await HandleMediaItemBaseAsync(movie, string.Join(", ", movie.Cast),
                                movie.AvailabilityByService.Keys.ToList(),
                                string.Join(", ", movie.MediaGenres.Select(mg => mg.Genre.Name)), null);

                        _context.Movies.Add(movie);
                    }
                    else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                    {
                        var series = await tmdbService.GetSeriesByIdAsync(tmdbId, region);
                        if (series != null)
                        {
                            await HandleMediaItemBaseAsync(series, string.Join(", ", series.Cast),
                                series.AvailabilityByService.Keys.ToList(),
                                string.Join(", ", series.MediaGenres.Select(mg => mg.Genre.Name)), null);

                            if (series.Seasons != null)
                            {
                                foreach (var season in series.Seasons)
                                {
                                    season.Series = series;
                                    if (season.Episodes != null)
                                        foreach (var ep in season.Episodes)
                                            ep.Season = season;
                                }
                            }

                            _context.Series.Add(series);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Message"] += $"Fehler bei ID {tmdbId}: {ex.Message}\n";
                }

            await _context.SaveChangesAsync();
            TempData["Message"] += "Multi-Import abgeschlossen.";
            return RedirectToAction(nameof(Index));
        }

    }
}
