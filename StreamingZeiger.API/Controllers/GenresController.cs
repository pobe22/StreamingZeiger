using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StreamingZeiger.Data;
using StreamingZeiger.Models;

namespace StreamingZeiger.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GenresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/genres
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            var genres = await _context.Genres
                .Include(g => g.MediaGenres)
                .ThenInclude(mg => mg.MediaItem)
                .ToListAsync();

            return Ok(genres);
        }

        // GET: api/genres/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Genre>> GetGenre(int id)
        {
            var genre = await _context.Genres
                .Include(g => g.MediaGenres)
                .ThenInclude(mg => mg.MediaItem)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null)
                return NotFound($"Genre mit ID {id} wurde nicht gefunden.");

            return Ok(genre);
        }

        // POST: api/genres
        [HttpPost]
        public async Task<ActionResult<Genre>> CreateGenre([FromBody] Genre genre)
        {
            if (string.IsNullOrWhiteSpace(genre.Name))
                return BadRequest("Genre-Name darf nicht leer sein.");

            try
            {
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGenre), new { id = genre.Id }, genre);
            }
            catch (DbUpdateException ex)
            {
                return BadRequest($"Fehler beim Erstellen: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // PUT: api/genres/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] Genre updated)
        {
            if (id != updated.Id)
                return BadRequest("ID in der URL und im Body stimmen nicht überein.");

            var existing = await _context.Genres.FindAsync(id);
            if (existing == null)
                return NotFound($"Genre mit ID {id} wurde nicht gefunden.");

            existing.Name = updated.Name;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return Conflict($"Fehler beim Aktualisieren: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // PATCH: api/genres/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchGenre(int id, [FromBody] Genre patch)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
                return NotFound($"Genre mit ID {id} wurde nicht gefunden.");

            if (!string.IsNullOrWhiteSpace(patch.Name))
                genre.Name = patch.Name;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(genre);
            }
            catch (DbUpdateException ex)
            {
                return Conflict($"Fehler beim Speichern: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // DELETE: api/genres/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres
                .Include(g => g.MediaGenres)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null)
                return NotFound($"Genre mit ID {id} wurde nicht gefunden.");

            // Wenn das Genre noch mit Medien verknüpft ist, löschen verhindern
            if (genre.MediaGenres.Any())
                return Conflict("Genre kann nicht gelöscht werden, da es noch Medien zugeordnet ist.");

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
