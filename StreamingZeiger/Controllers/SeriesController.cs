using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;

namespace StreamingZeiger.Controllers
{
    public class SeriesController : Controller
    {
        private readonly AppDbContext _context;

        public SeriesController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 12)
        {
            var total = await _context.Series.CountAsync();
            var items = await _context.Series
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var series = await _context.Series.FirstOrDefaultAsync(s => s.Id == id);
            if (series == null) return NotFound();
            return View(series);
        }
    }
}
