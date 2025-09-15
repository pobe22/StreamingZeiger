using StreamingZeiger.Models;
using System.Text.Json;

namespace StreamingZeiger.Services
{
    public class StaticMovieRepository : IStaticMovieRepository
    {
        private readonly List<Movie> _movies;
        public StaticMovieRepository(IWebHostEnvironment env)
        {
            var path = Path.Combine(env.WebRootPath, "data", "movies.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _movies = JsonSerializer.Deserialize<List<Movie>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Movie>();
            }
            else
            {
                _movies = new List<Movie>();
            }
        }
        public IEnumerable<Movie> GetAll() => _movies;
        public Movie? GetById(int id) => _movies.FirstOrDefault(m => m.Id == id);
        public void Add(Movie movie)
        {
            movie.Id = _movies.Count > 0 ? _movies.Max(m => m.Id) + 1 : 1;
            _movies.Add(movie);
        }
    }
}
