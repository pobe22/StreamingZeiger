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
    public class TmdbService: ITmdbService
    {
        private readonly string _apiKey;
        private readonly TMDbClient _client;
        private readonly Dictionary<int, string> _genreDictionary;

        public TmdbService()
        {
            _apiKey = Environment.GetEnvironmentVariable("TMDB_API_KEY")
                     ?? throw new Exception("TMDB_API_KEY is not set!");
            _client = new TMDbClient(_apiKey);
            var genres = _client.GetMovieGenresAsync();
            genres.Wait();
            _genreDictionary = genres.Result.ToDictionary(g => g.Id, g => g.Name);
        }

        // Einzelnen Film importieren
        public virtual async Task<Models.Movie> GetMovieByIdAsync(int tmdbId, string region)
        {
            int tmdbIdLocal = tmdbId;
            var movieDetails = await _client.GetMovieAsync(tmdbIdLocal, MovieMethods.Credits | MovieMethods.Videos);

            if (movieDetails == null)
            {
                return null; 
            }

            var movie = new Models.Movie
            {
                Title = movieDetails.Title,
                OriginalTitle = movieDetails.OriginalTitle,
                Description = movieDetails.Overview,
                DurationMinutes = movieDetails.Runtime ?? 0,
                Year = movieDetails.ReleaseDate?.Year ?? 0,
                PosterFile = movieDetails.PosterPath != null
                           ? $"https://image.tmdb.org/t/p/w500{movieDetails.PosterPath}"
                           : string.Empty,
                Cast = movieDetails.Credits?.Cast?.Select(c => c.Name).ToList() ?? new List<string>(),
                Director = movieDetails.Credits?.Crew?.FirstOrDefault(c => c.Job == "Director")?.Name ?? string.Empty,
                TrailerUrl = movieDetails.Videos?.Results?.FirstOrDefault()?.Key is string key && !string.IsNullOrEmpty(key)
                           ? $"https://www.youtube.com/embed/{key}"
                           : string.Empty,
                AvailabilityByService = new Dictionary<string, bool>()
            };

            // Genres als n:m abbilden
            movie.MediaGenres = movieDetails.Genres
                .Select(g => new MediaGenre { Genre = new Genre { Name = g.Name } })
                .ToList();

            await FillAvailabilityAsync(tmdbIdLocal, movie, region);

            return movie;
        }
        private async Task FillAvailabilityAsync(int tmdbIdLocal, Models.Movie movie, string region)
        {
            var providers = await _client.GetMovieWatchProvidersAsync(tmdbIdLocal);

            // Default: alle Services false
            movie.AvailabilityByService["Netflix"] = false;
            movie.AvailabilityByService["Disney+"] = false;
            movie.AvailabilityByService["Prime Video"] = false;

            if (providers?.Results != null && providers.Results.ContainsKey(region))
            {
                var regionData = providers.Results[region];

                IEnumerable<dynamic> flatrateList = Enumerable.Empty<dynamic>();
                var property = regionData.GetType().GetProperty("FlatRate"); 
                if (property != null)
                    flatrateList = property.GetValue(regionData) as IEnumerable<dynamic> ?? Enumerable.Empty<dynamic>();

                foreach (var p in flatrateList)
                {
                    string name = null;
                    try { name = p.ProviderName as string; } catch { }

                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.StartsWith("Netflix"))
                        movie.AvailabilityByService["Netflix"] = true;
                    else if (name.StartsWith("Disney"))
                        movie.AvailabilityByService["Disney+"] = true;
                    else if (name.StartsWith("Amazon Prime"))
                        movie.AvailabilityByService["Prime Video"] = true;
                }
            }
        }

        // Einzelne Serie importieren
        public async Task<Series> GetSeriesByIdAsync(int tmdbId, string region)
        {
            // Serien-Details mit Credits & Videos
            var seriesDetails = await _client.GetTvShowAsync(tmdbId, TvShowMethods.Credits | TvShowMethods.Videos);

            var series = new Series
            {
                Title = seriesDetails.Name,
                OriginalTitle = seriesDetails.OriginalName,
                StartYear = seriesDetails.FirstAirDate?.Year ?? 0,
                EndYear = seriesDetails.LastAirDate?.Year,
                Description = seriesDetails.Overview ?? "",
                PosterFile = !string.IsNullOrEmpty(seriesDetails.PosterPath)
                             ? "https://image.tmdb.org/t/p/w500" + seriesDetails.PosterPath
                             : "",
                Cast = seriesDetails.Credits?.Cast?.Select(c => c.Name).ToList() ?? new List<string>(),
                Director = seriesDetails.CreatedBy?.FirstOrDefault()?.Name ?? "",
                AvailabilityByService = new Dictionary<string, bool>()
            };

            // Trailer URL
            var trailerKey = seriesDetails.Videos.Results
                                .FirstOrDefault(v => v.Site == "YouTube" && v.Type == "Trailer")?.Key;
            series.TrailerUrl = !string.IsNullOrEmpty(trailerKey)
                                ? $"https://www.youtube.com/embed/{trailerKey}"
                                : "";

            // Genres
            series.MediaGenres = seriesDetails.Genres
                                    .Select(g => new MediaGenre { Genre = new Genre { Name = g.Name } })
                                    .ToList();

            // Seasons & Episodes
            series.Seasons = new List<Season>();
            foreach (var sInfo in seriesDetails.Seasons)
            {
                var seasonDetails = await _client.GetTvSeasonAsync(tmdbId, sInfo.SeasonNumber);

                var season = new Season
                {
                    SeasonNumber = seasonDetails.SeasonNumber,
                    Series = series,
                    Episodes = seasonDetails.Episodes.Select(e => new Episode
                    {
                        EpisodeNumber = e.EpisodeNumber,
                        Title = e.Name,
                        Description = e.Overview ?? "",
                        DurationMinutes = e.Runtime ?? 0,
                        Season = null
                    }).ToList()
                };

                // FK in Episodes setzen
                foreach (var ep in season.Episodes)
                    ep.Season = season;

                series.Seasons.Add(season);
            }

            // Streaming-Verfügbarkeit
            await FillSeriesAvailabilityAsync(tmdbId, series, region);

            return series;
        }

        // Hilfsmethode: Streaming-Verfügbarkeit
        private async Task FillSeriesAvailabilityAsync(int tmdbId, Series series, string region)
        {
            var providers = await _client.GetTvShowWatchProvidersAsync(tmdbId);

            // Default: alle Services false
            series.AvailabilityByService["Netflix"] = false;
            series.AvailabilityByService["Disney+"] = false;
            series.AvailabilityByService["Prime Video"] = false;

            if (providers?.Results != null && providers.Results.ContainsKey(region))
            {
                var regionData = providers.Results[region];

                IEnumerable<dynamic> flatrateList = Enumerable.Empty<dynamic>();
                var property = regionData.GetType().GetProperty("Flatrate");
                if (property != null)
                    flatrateList = property.GetValue(regionData) as IEnumerable<dynamic> ?? Enumerable.Empty<dynamic>();

                foreach (var p in flatrateList)
                {
                    string name = null;
                    try { name = p.ProviderName as string; } catch { }

                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.StartsWith("Netflix"))
                        series.AvailabilityByService["Netflix"] = true;
                    else if (name.StartsWith("Disney"))
                        series.AvailabilityByService["Disney+"] = true;
                    else if (name.StartsWith("Amazon Prime"))
                        series.AvailabilityByService["Prime Video"] = true;
                }
            }
        }
    }
}