using System;

namespace VentyTime.Shared.Models
{
    public class RegistrationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public EventRegistration? Registration { get; set; }
    }
}
