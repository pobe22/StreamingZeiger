using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;
using StreamingZeiger.Services;
using StreamingZeiger.Models;
using StreamingZeiger.Services;

namespace StreamingZeiger.Controllers
{
    public class AdminController : Controller
    {
        private readonly IStaticMovieRepository _repo;
        private readonly IWebHostEnvironment _env;

        public AdminController(IStaticMovieRepository repo, IWebHostEnvironment env)
        {
            _repo = repo;
            _env = env;
        }

        // GET: /Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Movie movie, string CastCsv, List<string> Services, IFormFile? PosterUpload)
        {
            if (!ModelState.IsValid)
            {
                return View(movie);
            }

            // Cast aus CSV
            if (!string.IsNullOrWhiteSpace(CastCsv))
            {
                movie.Cast = CastCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => c.Trim())
                                    .ToList();
            }

            // Services -> Availability
            foreach (var service in Services)
            {
                movie.AvailabilityByService[service] = true;
            }

            // Poster hochladen (optional)
            if (PosterUpload != null && PosterUpload.Length > 0)
            {
                var fileName = Path.GetFileName(PosterUpload.FileName);
                var savePath = Path.Combine(_env.WebRootPath, "images", "posters", fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    PosterUpload.CopyTo(stream);
                }

                movie.PosterFile = "/images/posters/" + fileName;
            }

            // In Repo speichern (Mock)
            _repo.Add(movie);

            TempData["Message"] = "Film erfolgreich hinzugefügt (Mock).";
            return RedirectToAction("Index", "Movies");
        }

        // Weitere Mock-Views: Edit, Delete, Import (optional)
        public IActionResult Index()
        {
            var movies = _repo.GetAll();
            return View(movies);
        }
    }
}
