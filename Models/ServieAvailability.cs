using Microsoft.AspNetCore.Mvc;

namespace StreamingZeiger.Models
{
    public class ServieAvailability : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
