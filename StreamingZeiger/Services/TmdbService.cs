using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;

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
            movie.MediaGenres = movieDetails.Genres
                .Select(g => new MediaGenre { Genre = new Genre { Name = g.Name } })
                .ToList();

            return movie;
        }

        // Einzelne Serie importieren
        public async Task<Models.Series> GetSeriesByIdAsync(int tmdbId)
        {
            var seriesDetails = await _client.GetTvShowAsync(tmdbId, TvShowMethods.Credits | TvShowMethods.Videos);

            var series = new Series
            {
                Title = seriesDetails.Name,
                OriginalTitle = seriesDetails.OriginalName,
                StartYear = seriesDetails.FirstAirDate?.Year ?? 0,
                EndYear = seriesDetails.LastAirDate?.Year,
                Seasons = seriesDetails.NumberOfSeasons,
                Episodes = seriesDetails.NumberOfEpisodes,
                Description = seriesDetails.Overview ?? "",
                PosterFile = "https://image.tmdb.org/t/p/w500" + (seriesDetails.PosterPath ?? ""),
                Cast = seriesDetails.Credits.Cast.Select(c => c.Name).ToList(),
                Director = seriesDetails.Credits.Crew
                            .Where(c => c.Job == "Director")
                            .Select(c => c.Name)
                            .FirstOrDefault() ?? ""
            };

            // Trailer-URL als YouTube-Embed setzen
            var trailerKey = seriesDetails.Videos.Results
                                .FirstOrDefault(v => v.Site == "YouTube" && v.Type == "Trailer")?.Key;

            series.TrailerUrl = !string.IsNullOrEmpty(trailerKey)
                                ? $"https://www.youtube.com/embed/{trailerKey}"
                                : "";

            // Genres als n:m abbilden
            series.MediaGenres = seriesDetails.Genres
                                    .Select(g => new MediaGenre { Genre = new Genre { Name = g.Name } })
                                    .ToList();

            return series;
        }
    }
}