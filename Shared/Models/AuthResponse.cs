using System;

namespace VentyTime.Shared.Models
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public AuthResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public AuthResponse(bool success, string message, string token)
        {
            Success = success;
            Message = message;
            Token = token;
        }

        public AuthResponse() { }
    }
}
