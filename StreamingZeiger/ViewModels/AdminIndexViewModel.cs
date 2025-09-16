using Microsoft.AspNetCore.Mvc;
using StreamingZeiger.Models;

namespace StreamingZeiger.ViewModels
{
    public class AdminIndexViewModel
    {
        public List<Movie> Movies { get; set; } = new();
        public List<Series> Series { get; set; } = new();
    }

}
