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

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var vm = new AdminIndexViewModel
            {
                Movies = await _context.Movies
                    .Include(m => m.MovieGenres)
                    .ThenInclude(mg => mg.Genre)
                    .ToListAsync(),

                Series = await _context.Series
                    .Include(s => s.SeriesGenres)
                    .ThenInclude(sg => sg.Genre)
                    .ToListAsync()
            };

            return View(vm);
        }

        // GET: Admin/Create
        public IActionResult CreateMovie()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovie(
    Movie movie,
    string CastCsv,
    List<string> Services,
    string GenreCsv,
    IFormFile? PosterUpload)
        {
            if (!ModelState.IsValid)
                return View(movie);

            // Cast übernehmen
            if (!string.IsNullOrWhiteSpace(CastCsv))
                movie.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim())
                                    .ToList();

            // Streamingdienste übernehmen
            movie.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            // Poster speichern
            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var extension = Path.GetExtension(PosterUpload.FileName);
                var fileName = Path.GetFileNameWithoutExtension(PosterUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                movie.PosterFile = "/images/posters/" + uniqueFileName;
            }

            // Genres übernehmen
            if (!string.IsNullOrWhiteSpace(GenreCsv))
            {
                var genreNames = GenreCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(g => g.Trim())
                                         .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var name in genreNames)
                {
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

                    if (genre == null)
                    {
                        genre = new Genre { Name = name };
                        _context.Genres.Add(genre);
                    }

                    movie.MovieGenres.Add(new MovieGenre { Movie = movie, Genre = genre });
                }
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Film erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> EditMovie(int id)
        {
            var movie = await _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            ViewBag.CastCsv = movie.Cast != null ? string.Join(", ", movie.Cast) : "";
            ViewBag.GenreCsv = string.Join(", ", movie.MovieGenres.Select(mg => mg.Genre.Name));

            return View(movie);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMovie(
            int id,
            Movie movie,
            string CastCsv,
            List<string> Services,
            string GenreCsv,
            IFormFile? PosterUpload)
        {
            if (id != movie.Id) return NotFound();
            if (!ModelState.IsValid) return View(movie);

            // Cast übernehmen
            if (!string.IsNullOrWhiteSpace(CastCsv))
                movie.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim())
                                    .ToList();

            // Dienste
            movie.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            // Poster neu speichern
            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var extension = Path.GetExtension(PosterUpload.FileName);
                var fileName = Path.GetFileNameWithoutExtension(PosterUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                movie.PosterFile = "/images/posters/" + uniqueFileName;
            }

            // MovieGenres neu setzen
            var existingMovie = await _context.Movies
                .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingMovie == null) return NotFound();

            // Basiswerte updaten
            _context.Entry(existingMovie).CurrentValues.SetValues(movie);

            // Alte Zuordnungen löschen
            existingMovie.MovieGenres.Clear();

            // Neue Genres setzen
            if (!string.IsNullOrWhiteSpace(GenreCsv))
            {
                var genreNames = GenreCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(g => g.Trim())
                                         .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var name in genreNames)
                {
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

                    if (genre == null)
                    {
                        genre = new Genre { Name = name };
                        _context.Genres.Add(genre);
                    }

                    existingMovie.MovieGenres.Add(new MovieGenre { Movie = existingMovie, Genre = genre });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Film erfolgreich bearbeitet.";
            return RedirectToAction("Details", new { id = movie.Id });
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> DeleteMovie(int? id)
        {
            if (id == null)
                return NotFound();

            var movie = await _context.Movies
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            return View(movie);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMovieConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/CreateSeries
        public IActionResult CreateSeries()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeries(
            Series series,
            string CastCsv,
            List<string> Services,
            string GenreCsv,
            IFormFile? PosterUpload)
        {
            if (!ModelState.IsValid)
                return View(series);

            // Cast übernehmen
            if (!string.IsNullOrWhiteSpace(CastCsv))
                series.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(c => c.Trim())
                                     .ToList();

            // Dienste übernehmen
            series.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            // Poster speichern
            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var extension = Path.GetExtension(PosterUpload.FileName);
                var fileName = Path.GetFileNameWithoutExtension(PosterUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                series.PosterFile = "/images/posters/" + uniqueFileName;
            }

            // Genres übernehmen
            if (!string.IsNullOrWhiteSpace(GenreCsv))
            {
                var genreNames = GenreCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(g => g.Trim())
                                         .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var name in genreNames)
                {
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

                    if (genre == null)
                    {
                        genre = new Genre { Name = name };
                        _context.Genres.Add(genre);
                    }

                    series.SeriesGenres.Add(new SeriesGenre { Series = series, Genre = genre });
                }
            }

            _context.Series.Add(series);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/EditSeries/5
        public async Task<IActionResult> EditSeries(int id)
        {
            var series = await _context.Series
                .Include(s => s.SeriesGenres)
                .ThenInclude(sg => sg.Genre)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
                return NotFound();

            ViewBag.CastCsv = series.Cast != null ? string.Join(", ", series.Cast) : "";
            ViewBag.GenreCsv = string.Join(", ", series.SeriesGenres.Select(sg => sg.Genre.Name));

            return View(series);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeries(
            int id,
            Series series,
            string CastCsv,
            List<string> Services,
            string GenreCsv,
            IFormFile? PosterUpload)
        {
            if (id != series.Id) return NotFound();
            if (!ModelState.IsValid) return View(series);

            // Cast übernehmen
            if (!string.IsNullOrWhiteSpace(CastCsv))
                series.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(c => c.Trim())
                                     .ToList();

            // Dienste
            series.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            // Poster neu speichern
            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var extension = Path.GetExtension(PosterUpload.FileName);
                var fileName = Path.GetFileNameWithoutExtension(PosterUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                series.PosterFile = "/images/posters/" + uniqueFileName;
            }

            // Vorhandene Serie laden
            var existingSeries = await _context.Series
                .Include(s => s.SeriesGenres)
                .ThenInclude(sg => sg.Genre)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (existingSeries == null) return NotFound();

            // Basiswerte aktualisieren
            _context.Entry(existingSeries).CurrentValues.SetValues(series);

            // Alte Genre-Zuordnungen löschen
            existingSeries.SeriesGenres.Clear();

            // Neue Genres setzen
            if (!string.IsNullOrWhiteSpace(GenreCsv))
            {
                var genreNames = GenreCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(g => g.Trim())
                                         .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var name in genreNames)
                {
                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

                    if (genre == null)
                    {
                        genre = new Genre { Name = name };
                        _context.Genres.Add(genre);
                    }

                    existingSeries.SeriesGenres.Add(new SeriesGenre { Series = existingSeries, Genre = genre });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Serie erfolgreich bearbeitet.";
            return RedirectToAction("DetailsSeries", new { id = series.Id });
        }

        // GET: Admin/DeleteSeries/5
        public async Task<IActionResult> DeleteSeries(int? id)
        {
            if (id == null)
                return NotFound();

            var series = await _context.Series
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
                return NotFound();

            return View(series);
        }

        // POST: Admin/DeleteSeries/5
        [HttpPost, ActionName("DeleteSeries")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeriesConfirmed(int id)
        {
            var series = await _context.Series.FindAsync(id);
            if (series != null)
            {
                _context.Series.Remove(series);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ImportFromTmdb(int tmdbId)
        {
            var tmdbService = new TmdbService();
            var movie = await tmdbService.GetMovieByIdAsync(tmdbId);

            return Json(movie); // Liefert die Daten als JSON an das Frontend
        }

        [HttpGet]
        public async Task<IActionResult> ImportSeriesFromTmdb(int tmdbId)
        {
            var tmdbService = new TmdbService();
            var series = await tmdbService.GetSeriesByIdAsync(tmdbId);

            if (series == null)
                return NotFound(new { message = "Serie nicht gefunden" });

            // Nur die relevanten Felder zurückgeben (damit sie ins Formular passen)
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
                genres = series.SeriesGenres?.Select(sg => sg.Genre.Name).ToList()
            });
        }


    }
}
