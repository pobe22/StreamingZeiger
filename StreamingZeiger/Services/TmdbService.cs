using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StreamingZeiger.Services
{
    public class TmdbService
    {
        private readonly string _apiKey = "3263dddf46e4c2af1440296270193547";
        private readonly TMDbClient _client;
        private readonly Dictionary<int, string> _genreDictionary;

        public TmdbService()
        {
            _client = new TMDbClient(_apiKey);
            var genres = _client.GetMovieGenresAsync();
            genres.Wait();
            _genreDictionary = genres.Result.ToDictionary(g => g.Id, g => g.Name);
        }

        // Einzelnen Film importieren
        public async Task<Models.Movie> GetMovieByIdAsync(int tmdbId)
        {
            var movieDetails = await _client.GetMovieAsync(tmdbId, MovieMethods.Credits | MovieMethods.Videos);

            var movie = new Models.Movie
            {
                Title = movieDetails.Title,
                OriginalTitle = movieDetails.OriginalTitle,
                Description = movieDetails.Overview,
                Year = movieDetails.ReleaseDate?.Year ?? 0,
                PosterFile = $"https://image.tmdb.org/t/p/w500{movieDetails.PosterPath}",
                TrailerUrl = movieDetails.Videos.Results.FirstOrDefault()?.Key ?? string.Empty,
                Cast = movieDetails.Credits.Cast.Select(c => c.Name).ToList(),
                Director = movieDetails.Credits.Crew.FirstOrDefault(c => c.Job == "Director")?.Name ?? string.Empty
            };

            // Genres als n:m abbilden
            movie.MovieGenres = movieDetails.Genres
                .Select(g => new MovieGenre { Genre = new Models.Genre { Name = g.Name } })
                .ToList();

            return movie;
        }
    }
}
