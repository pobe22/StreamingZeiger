using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace StreamingZeiger.Models
{
    public class Series : MediaItem
    {
        public int StartYear { get; set; }
        public int? EndYear { get; set; }
        public int Seasons { get; set; }
        public int Episodes { get; set; }
    }
}
