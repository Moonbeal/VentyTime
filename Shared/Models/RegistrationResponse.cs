using System;

namespace VentyTime.Shared.Models
{
    public class RegistrationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime? RegistrationDate { get; set; }
        public int? RegistrationId { get; set; }

        public RegistrationResponse()
        {
            Message = string.Empty;
        }

        public RegistrationResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
