using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models.Auth;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string Bio { get; set; } = string.Empty;
}
