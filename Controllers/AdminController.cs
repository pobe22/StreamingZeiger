using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
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
            var movies = await _context.Movies.ToListAsync();
            return View(movies);
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
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
        public async Task<IActionResult> Edit(int id)
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
        public async Task<IActionResult> Edit(
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
        public async Task<IActionResult> Delete(int? id)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
