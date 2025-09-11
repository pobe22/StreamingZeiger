using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
    public class SeriesController : Controller
    {
        private static List<Series> _series = new List<Series>
        {
            new Series { Id = 1, Title = "Breaking Bad", Description = "Chemielehrer wird Drogenboss", Genre = "Drama", Seasons = 5, Episodes = 62, PosterUrl = "/images/breakingbad.jpg" },
            new Series { Id = 2, Title = "Stranger Things", Description = "Mystery in Hawkins", Genre = "Sci-Fi", Seasons = 4, Episodes = 34, PosterUrl = "/images/strangerthings.jpg" }
        };

        public IActionResult Index(int page = 1, int pageSize = 12)
        {
            var total = _series.Count;
            var items = _series
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(items);
        }

        public IActionResult Details(int id)
        {
            var series = _series.FirstOrDefault(s => s.Id == id);
            if (series == null) return NotFound();
            return View(series);
        }
    }
}
