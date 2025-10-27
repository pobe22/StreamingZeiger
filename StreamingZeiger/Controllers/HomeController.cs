using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Models;
using System.Diagnostics;
using StreamingZeiger.Data;
using StreamingZeiger.ViewModels;

namespace StreamingZeiger.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var featuredMovies = await _context.Movies
                .Include(m => m.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .OrderByDescending(m => m.Rating)
                .Take(3)
                .ToListAsync();

            var seriesList = await _context.Series
                .Include(s => s.MediaGenres)
                    .ThenInclude(mg => mg.Genre)
                .Include(s => s.Seasons)
                    .ThenInclude(season => season.Episodes)
                .ToListAsync();

            var viewModel = new AdminIndexViewModel
            {
                Movies = featuredMovies,
                Series = seriesList
            };

            return View(viewModel);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
