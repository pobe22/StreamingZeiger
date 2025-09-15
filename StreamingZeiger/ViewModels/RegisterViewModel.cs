using System.ComponentModel.DataAnnotations;

namespace StreamingZeiger.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; }
    }
}
