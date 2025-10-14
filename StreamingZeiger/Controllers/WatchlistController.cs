using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using StreamingZeiger.ViewModels;
using System.Threading.Tasks;

namespace StreamingZeiger.Controllers
{
    //Aufgabe 1. Zugriff nur für angemeldete Benutzer
    [Authorize]
    public class WatchlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMemoryCache _cache;

        public WatchlistController(AppDbContext context, UserManager<ApplicationUser> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        //Aufgabe 7. Watchlist anzeigen
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var items = await _context.WatchlistItems
                .Include(w => w.MediaItem)
                .ThenInclude(mi => mi.MediaGenres) 
                .ThenInclude(mg => mg.Genre)
                .Where(w => w.UserId == user.Id)
                .ToListAsync();

            var viewModel = new WatchlistViewModel
            {
                Movies = items.Where(w => w.MediaItem is Movie).ToList(),
                Series = items.Where(w => w.MediaItem is Series).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Add(int mediaItemId, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var mediaItem = await _context.MediaItems.FindAsync(mediaItemId);
            if (mediaItem == null)
            {
                return NotFound("MediaItem nicht gefunden.");
            }

            if (!await _context.WatchlistItems
                .AnyAsync(w => w.UserId == user.Id && w.MediaItemId == mediaItemId))
            {
                _context.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = user.Id,
                    MediaItemId = mediaItemId
                });

                await _context.SaveChangesAsync();
            }

            return Redirect(returnUrl ?? "/");
        }

        public async Task<IActionResult> Remove(int mediaItemId, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var item = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.MediaItemId == mediaItemId);

            if (item != null)
            {
                _context.WatchlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return Redirect(returnUrl ?? "/");
        }

        //Aufgabe 2. AJAX
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Toggle([FromBody] WatchlistToggleRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "Nicht eingeloggt" });

            var mediaItemId = request.MediaItemId;

            //Aufgabe 4. Backend-Logik
            var existing = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.MediaItemId == mediaItemId);

            bool added;

            if (existing == null)
            {
                _context.WatchlistItems.Add(new WatchlistItem
                {
                    UserId = user.Id,
                    MediaItemId = mediaItemId
                });
                added = true;
            }
            else
            {
                _context.WatchlistItems.Remove(existing);
                added = false;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, added });
        }

        public class WatchlistToggleRequest
        {
            public int MediaItemId { get; set; }
        }

    }
}