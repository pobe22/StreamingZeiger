using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
    public class WatchlistController : Controller
    {
        private readonly List<Movie> _movies;

        public WatchlistController()
        {
            // Dummy Daten (später aus DB)
            _movies = new List<Movie>
            {
                new Movie { Id = 1, Title = "Inception", Year = 2010, PosterFile="/images/posters/inception.jpg" },
                new Movie { Id = 2, Title = "Matrix", Year = 1999, PosterFile="/images/posters/matrix.jpg" },
                new Movie { Id = 3, Title = "Interstellar", Year = 2014, PosterFile="/images/posters/interstellar.jpg" }
            };
        }

        public IActionResult Index()
        {
            var list = HttpContext.Session.GetObjectFromJson<List<Movie>>("Watchlist") ?? new List<Movie>();
            return View(list);
        }

        public IActionResult Add(int id)
        {
            var list = HttpContext.Session.GetObjectFromJson<List<Movie>>("Watchlist") ?? new List<Movie>();
            var movie = _movies.FirstOrDefault(m => m.Id == id);

            if (movie != null && !list.Any(m => m.Id == id))
            {
                list.Add(movie);
                HttpContext.Session.SetObjectAsJson("Watchlist", list);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var list = HttpContext.Session.GetObjectFromJson<List<Movie>>("Watchlist") ?? new List<Movie>();
            var movie = list.FirstOrDefault(m => m.Id == id);

            if (movie != null)
            {
                list.Remove(movie);
                HttpContext.Session.SetObjectAsJson("Watchlist", list);
            }

            return RedirectToAction("Index");
        }
    }
}
