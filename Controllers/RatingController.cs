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
    public class RatingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Add(int movieId, int score)
        {
            var user = await _userManager.GetUserAsync(User);
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.MovieId == movieId && r.UserId == user.Id);

            if (rating == null)
            {
                rating = new Rating
                {
                    MovieId = movieId,
                    UserId = user.Id,
                    Score = score
                };
                _context.Ratings.Add(rating);
            }
            else
            {
                rating.Score = score;
                _context.Ratings.Update(rating);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Movies", new { id = movieId });
        }
    }
}
