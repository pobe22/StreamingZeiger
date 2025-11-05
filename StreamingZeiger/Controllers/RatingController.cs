using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Data;
using StreamingZeiger.Models;
using Microsoft.EntityFrameworkCore;

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
    public async Task<IActionResult> Add(int mediaItemId, int score, string? returnUrl = null)
    { 
        if (ModelState.IsValid == false || score < 1 || score > 10)
        {
            return BadRequest("Invalid rating score.");
        }
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var rating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.MediaItemId == mediaItemId && r.UserId == user.Id);

        if (rating == null)
        {
            rating = new Rating
            {
                MediaItemId = mediaItemId,
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

        await _context.UpdateAverageRatingAsync(mediaItemId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Details", "Movies", new { id = mediaItemId });
    }

    [HttpPost]
    public async Task<IActionResult> AddAjax([FromBody] RatingDto dto)
    {
        if (ModelState.IsValid == false || dto.Score < 1 || dto.Score > 10)
        {
            return BadRequest("Invalid rating score.");
        }
        var user = await _userManager.GetUserAsync(User);

        if (user == null || !User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var rating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.MediaItemId == dto.MediaItemId && r.UserId == user.Id);

        if (rating == null)
        {
            rating = new Rating { MediaItemId = dto.MediaItemId, UserId = user.Id, Score = dto.Score };
            _context.Ratings.Add(rating);
        }
        else
        {
            rating.Score = dto.Score;
            _context.Ratings.Update(rating);
        }    

        await _context.SaveChangesAsync();

        await _context.UpdateAverageRatingAsync(dto.MediaItemId);

        var ratings = await _context.Ratings.Where(r => r.MediaItemId == dto.MediaItemId).ToListAsync();
        var avg = ratings.Average(r => r.Score);

        return Json(new { userScore = dto.Score, average = avg, votes = ratings.Count });
    }

    public class RatingDto
    {
        public int MediaItemId { get; set; }
        public int Score { get; set; }
    }
}