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

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Movie movie, string CastCsv, List<string> Services, IFormFile? PosterUpload)
        {
            if (!ModelState.IsValid)
                return View(movie);

            if (!string.IsNullOrWhiteSpace(CastCsv))
                movie.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim())
                                    .ToList();

            movie.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var fileName = Path.GetFileName(PosterUpload.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                movie.PosterFile = "/images/posters/" + fileName;
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Film erfolgreich hinzugefügt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();

            ViewBag.CastCsv = movie.Cast != null ? string.Join(", ", movie.Cast) : "";
            return View(movie);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Movie movie, string CastCsv, List<string> Services, IFormFile? PosterUpload)
        {
            if (id != movie.Id) return NotFound();
            if (!ModelState.IsValid) return View(movie);

            if (!string.IsNullOrWhiteSpace(CastCsv))
                movie.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim())
                                    .ToList();

            movie.AvailabilityByService = Services.ToDictionary(s => s, s => true);

            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var fileName = Path.GetFileName(PosterUpload.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                    await PosterUpload.CopyToAsync(stream);

                movie.PosterFile = "/images/posters/" + fileName;
            }

            _context.Update(movie);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Film erfolgreich bearbeitet.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();
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
                TempData["Message"] = "Film erfolgreich gelöscht.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
