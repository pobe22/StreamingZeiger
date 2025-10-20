using StreamingZeiger.Models;

namespace StreamingZeiger.Services
{
    public interface ITmdbService
    {
        Task<Movie> GetMovieByIdAsync(int id, string region);
        Task<Series> GetSeriesByIdAsync(int id, string region);

        Task<int?> SearchMovieIdByTitleAsync(string title, string region);
        Task<int?> SearchSeriesIdByTitleAsync(string title, string region);

        Task<List<int>> GetTopMoviesAsync(string region);
        Task<List<int>> GetTopSeriesAsync(string region);
    }
}
