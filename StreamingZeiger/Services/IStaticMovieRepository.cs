using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;

namespace StreamingZeiger.Services
{
    public interface IStaticMovieRepository
    {
        IEnumerable<Movie> GetAll();
        Movie? GetById(int id);
        void Add(Movie movie);
    }
}
