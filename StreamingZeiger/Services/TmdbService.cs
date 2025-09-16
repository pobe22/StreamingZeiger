using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.TvShows;
using TMDbLib.Objects.Search;

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
        public async Task<Series?> GetSeriesByIdAsync(int tmdbId)
        {
            var seriesDetails = await _client.GetTvShowAsync(
                tmdbId,
                TvShowMethods.Credits | TvShowMethods.Videos
            );

            if (seriesDetails == null)
                return null;

            var series = new Series
            {
                Title = seriesDetails.Name,
                OriginalTitle = seriesDetails.OriginalName,
                Description = seriesDetails.Overview,
                StartYear = seriesDetails.FirstAirDate?.Year ?? 0,
                EndYear = seriesDetails.LastAirDate?.Year,
                Seasons = seriesDetails.NumberOfSeasons,
                Episodes = seriesDetails.NumberOfEpisodes,
                PosterFile = $"https://image.tmdb.org/t/p/w500{seriesDetails.PosterPath}",
                TrailerUrl = seriesDetails.Videos?.Results.FirstOrDefault()?.Key ?? string.Empty,
                Cast = seriesDetails.Credits?.Cast?.Select(c => c.Name).ToList() ?? new List<string>(),
                Director = seriesDetails.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? string.Empty
            };

            // Genres als n:m abbilden
            series.SeriesGenres = seriesDetails.Genres
                .Select(g => new SeriesGenre { Genre = new Models.Genre { Name = g.Name } })
                .ToList();

            return series;
        }
    }
}
