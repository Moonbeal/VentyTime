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
    }
}
