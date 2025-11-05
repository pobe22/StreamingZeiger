using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.ViewModels;
using TMDbLib.Objects.Movies;

namespace StreamingZeiger.Controllers
{
    [Authorize(Roles = "Admin, Redakteur")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ITmdbService _tmdbService;
        private readonly LoggingService _loggingService;
        private readonly DatabaseBackupService _backupService;

        public AdminController(AppDbContext context, IWebHostEnvironment env,
                          DynamicDbContextFactory contextFactory, ITmdbService tmdb, LoggingService loggingService, DatabaseBackupService backupService)
        {
            _context = context;
            _env = env;
            _contextFactory = contextFactory;
            _tmdbService = tmdb;
            _loggingService = loggingService;
            _backupService = backupService;
        }

        // Übersicht
        public async Task<IActionResult> Index()
        {
            var context = _contextFactory != null
                ? await _contextFactory.CreateDbContextAsync()
                : _context;

            var vm = new AdminIndexViewModel
            {
                Movies = await context.Movies
                    .Include(m => m.MediaGenres)
                        .ThenInclude(mg => mg.Genre)
                    .ToListAsync(),

                Series = await context.Series
                    .Include(s => s.MediaGenres)
                        .ThenInclude(mg => mg.Genre)
                    .ToListAsync()
            };

            return View(vm);
        }

        // ---------- Gemeinsamer Create-Helper ----------
        private async Task HandleMediaItemBaseAsync(MediaItem item, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            // --- Cast ---
            if (!string.IsNullOrWhiteSpace(castCsv))
                item.Cast = castCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(c => c.Trim())
                                   .Distinct()
                                   .ToList();

            // --- Dienste ---
            item.AvailabilityByService = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (services != null)
            {
                foreach (var s in services)
                {
                    var key = s?.Trim();
                    if (!string.IsNullOrEmpty(key) && !item.AvailabilityByService.ContainsKey(key))
                    {
                        item.AvailabilityByService[key] = true;
                    }
                }
            }

            // --- Poster ---
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

            // --- Genres ---
            if (!string.IsNullOrWhiteSpace(genreCsv))
            {
                var genreNames = genreCsv
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => !string.IsNullOrEmpty(g))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingGenres = await _context.Genres
                    .ToListAsync();

                var genreDict = existingGenres
                    .GroupBy(g => g.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var name in genreNames)
                {
                    var normalizedName = name.Trim().ToLowerInvariant();

                    if (!genreDict.TryGetValue(normalizedName, out var genre))
                    {
                        // Genre nur dem Kontext hinzufügen, SaveChanges später
                        genre = new Genre { Name = name.Trim() };
                        _context.Genres.Add(genre);
                        genreDict[normalizedName] = genre;
                    }

                    // MediaGenre hinzufügen
                    if (!item.MediaGenres.Any(mg => mg.Genre == genre))
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
        public async Task<IActionResult> CreateMovie(Models.Movie movie, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
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
            if (ModelState.IsValid == false)
            {
                return View();
            }
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
        public async Task<IActionResult> EditMovie(int id, Models.Movie movie, string castCsv, List<string> services, string genreCsv, IFormFile? posterUpload)
        {
            if (ModelState.IsValid == false)
            {
                return View(movie);
            }
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
            if (ModelState.IsValid == false)
            {
                return View();
            }
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
            if (ModelState.IsValid == false)
            {
                return View();
            }
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
            if (ModelState.IsValid == false)
            {
                return View(series);
            }
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
            if (ModelState.IsValid == false)
            {
                return View();
            }
            var series = await _context.Series
                .Include(s => s.Seasons).ThenInclude(se => se.Episodes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null) return NotFound();

            _context.Series.Remove(series); 
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportMultiple(string? tmdbIds, string type, IFormFile? csvFile, string? titles, bool importTop25 = false, string region = "DE")
        {
            if (ModelState.IsValid == false)
            {
                return View();
            }

            List<int> ids = new List<int>();

            if (importTop25)
            {
                if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
                    ids = await _tmdbService.GetTopMoviesAsync(region);
                else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                    ids = await _tmdbService.GetTopSeriesAsync(region);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tmdbIds))
                    return NotFound();

                // IDs aus Textfeld oder CSV-Datei auflösen
                ids = await ResolveIdsAsync(tmdbIds, csvFile, type, region);
                
                // IDs aus Titel-Textfeld auflösen
                if (!string.IsNullOrWhiteSpace(titles))
                {
                    var titleList = titles.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(t => t.Trim())
                                          .ToList();
                    foreach (var title in titleList)
                    {
                        int? resolvedId = type.ToLower() == "movie"
                            ? await _tmdbService.SearchMovieIdByTitleAsync(title, region)
                            : await _tmdbService.SearchSeriesIdByTitleAsync(title, region);

                        if (resolvedId.HasValue)
                            ids.Add(resolvedId.Value);
                    }
                }
            }

            if (!ids.Any())
            {
                TempData["Message"] = "Keine gültigen TMDB-IDs oder Titel gefunden.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var tmdbId in ids)
            {
                await ImportSingleAsync(tmdbId, type, region);
            }

            TempData["Message"] = "Multi-Import abgeschlossen.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ImportMultipleAjax([FromForm] string tmdbIds, [FromForm] IFormFile? csvFile, [FromForm] string type, [FromForm] string region = "DE")
        {
            if (ModelState.IsValid == false)
            {
                return View();
            }

            Response.ContentType = "text/plain";
            var ids = await ResolveIdsAsync(tmdbIds, csvFile, type, region);

            int total = ids.Count;
            int processed = 0;

            foreach (var tmdbId in ids)
            {
                bool success = await ImportSingleAsync(tmdbId, type, region);

                if (success)
                {
                    processed++;
                    await Response.WriteAsync($"PROGRESS:{processed}/{total}\n");
                    await Response.Body.FlushAsync();
                }
            }

            return new EmptyResult();
        }

        // --- private Hilfsmethoden ---

        private async Task<List<int>> ResolveIdsAsync(string tmdbIds, IFormFile? csvFile, string type, string region)
        {
            var ids = new List<int>();

            // IDs aus Textfeld
            if (!string.IsNullOrWhiteSpace(tmdbIds))
            {
                ids.AddRange(tmdbIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
                            .Where(id => id > 0));
            }

            // IDs aus CSV-Datei
            if (csvFile != null && csvFile.Length > 0)
            {
                using var reader = new StreamReader(csvFile.OpenReadStream());
                bool firstLine = true;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) continue;

                    if (firstLine) { firstLine = false; continue; } // Header überspringen

                    var columns = ParseCsvLine(line);
                    if (columns.Length < 2) continue;

                    var title = columns[1].Trim('"');
                    if (string.IsNullOrEmpty(title)) continue;

                    int? foundId = null;
                    if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
                        foundId = await _tmdbService.SearchMovieIdByTitleAsync(title, region);
                    else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                        foundId = await _tmdbService.SearchSeriesIdByTitleAsync(title, region);

                    if (foundId.HasValue)
                        ids.Add(foundId.Value);
                    else
                        await _loggingService.LogAsync("TMDB-Suche", $"Titel '{title}' nicht gefunden.");
                }
            }

            return ids.Distinct().ToList();
        }

        private async Task<bool> ImportSingleAsync(int tmdbId, string type, string region)
        {
            bool success = false;

            await ExecuteInTransactionAsync(async () =>
            {
                if (string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _context.Movies.AnyAsync(m => m.TmdbId == tmdbId);
                    if (exists)
                    {
                        await _loggingService.LogAsync("TMDB-Import übersprungen", $"Film mit TMDB-ID {tmdbId} existiert bereits.");
                        return;
                    }

                    var movie = await _tmdbService.GetMovieByIdAsync(tmdbId, region);
                    if (movie == null)
                    {
                        await _loggingService.LogAsync("TMDB-Importfehler", $"Film mit TMDB-ID {tmdbId} nicht gefunden.");
                        return;
                    }

                    // --- Movie vorbereiten ---
                    try
                    {
                        await HandleMediaItemBaseAsync(
                            movie,
                            string.Join(", ", movie.Cast),
                            movie.AvailabilityByService?.Keys?.ToList() ?? new List<string>(),
                            movie.MediaGenres != null ? string.Join(", ", movie.MediaGenres.Select(mg => mg.Genre.Name)) : "",
                            null
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Fehler beim HandleMediaItemBaseAsync: " + ex);
                        throw;
                    }


                    movie.TmdbId = tmdbId;
                    _context.Movies.Add(movie);
                    await _context.SaveChangesAsync();

                    await _loggingService.LogAsync("TMDB-Import erfolgreich", $"Film '{movie.Title}' (TMDB-ID {tmdbId}) importiert.");
                    success = true;
                }
                else if (string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
                {
                    var exists = await _context.Series.AnyAsync(s => s.TmdbId == tmdbId);
                    if (exists)
                    {
                        await _loggingService.LogAsync("TMDB-Import übersprungen", $"Serie mit TMDB-ID {tmdbId} existiert bereits.");
                        return;
                    }

                    var series = await _tmdbService.GetSeriesByIdAsync(tmdbId, region);
                    if (series == null)
                    {
                        await _loggingService.LogAsync("TMDB-Importfehler", $"Serie mit TMDB-ID {tmdbId} nicht gefunden.");
                        return;
                    }

                    // --- Series vorbereiten ---
                    await HandleMediaItemBaseAsync(
                        series,
                        string.Join(", ", series.Cast ?? new List<string>()),
                        series.AvailabilityByService?.Keys?.ToList() ?? new List<string>(),
                        series.MediaGenres != null ? string.Join(", ", series.MediaGenres.Select(mg => mg.Genre.Name)) : "",
                        null
                    );

                    // Seasons & Episodes: FK korrekt setzen
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

                    series.TmdbId = tmdbId;
                    _context.Series.Add(series);
                    await _context.SaveChangesAsync();

                    await _loggingService.LogAsync("TMDB-Import erfolgreich", $"Serie '{series.Title}' (TMDB-ID {tmdbId}) importiert.");
                    success = true;
                }
            });

            return success;
        }

        private async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            if (_context.Database.IsInMemory())
            {
                await action();
                return;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            await action();
            await transaction.CommitAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Backup()
        {
            await _backupService.PerformBackupAsync();
            TempData["Message"] = "Backup erfolgreich erstellt!";
            return RedirectToAction(nameof(Index));
        }


        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var value = "";

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(value);
                    value = "";
                }
                else
                {
                    value += c;
                }
            }
            result.Add(value);
            return result.ToArray();
        }


    }
}
