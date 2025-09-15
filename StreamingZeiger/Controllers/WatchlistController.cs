using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using System.Threading.Tasks;

namespace StreamingZeiger.Controllers
{
    [Authorize]
    public class WatchlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WatchlistController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var items = await _context.WatchlistItems
                .Include(w => w.Movie)
                .Where(w => w.UserId == user.Id)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Add(int movieId, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!await _context.WatchlistItems.AnyAsync(w => w.UserId == user.Id && w.MovieId == movieId))
            {
                _context.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = user.Id,
                    MovieId = movieId
                });
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Movies");
        }

        public async Task<IActionResult> Remove(int movieId, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _context.WatchlistItems.FirstOrDefaultAsync(w => w.UserId == user.Id && w.MovieId == movieId);

            if (item != null)
            {
                _context.WatchlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }
    }
}