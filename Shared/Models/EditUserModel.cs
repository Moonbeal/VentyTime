using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class EditUserModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }
}
