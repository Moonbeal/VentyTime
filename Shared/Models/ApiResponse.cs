namespace VentyTime.Shared.Models
{
    public class ApiResponse<T>
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }
}
