using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models.Auth
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public UserRole SelectedRole { get; set; }
    }
}
