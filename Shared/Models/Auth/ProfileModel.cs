using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models.Auth
{
    public class ProfileModel
    {
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        [Url(ErrorMessage = "Invalid website URL")]
        public string Website { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;
    }
}
