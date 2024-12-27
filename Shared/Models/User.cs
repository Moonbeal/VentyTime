namespace VentyTime.Shared.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public UserRole Role { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public List<string> OrganizedEventIds { get; set; } = new();
        public List<string> RegisteredEventIds { get; set; } = new();
    }
}
