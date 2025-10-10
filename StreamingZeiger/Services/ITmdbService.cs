using StreamingZeiger.Models;

namespace StreamingZeiger.Services
{
    public interface ITmdbService
    {
        Task<Movie> GetMovieByIdAsync(int id, string region);
        Task<Series> GetSeriesByIdAsync(int id, string region);
    }
}
