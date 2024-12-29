using System.Net.Http;
using System.Threading.Tasks;

namespace VentyTime.Client.Extensions
{
    public static class HttpRequestExceptionExtensions
    {
        public static async Task<HttpRequestException> CreateFromResponseAsync(HttpResponseMessage response)
        {
            var message = await response.Content.ReadAsStringAsync();
            var exception = new HttpRequestException($"HTTP {(int)response.StatusCode}: {message}");
            exception.Data["ResponseBody"] = message;
            return exception;
        }

        public static Task<string> GetResponseBodyAsync(this HttpRequestException exception)
        {
            if (exception.Data.Contains("ResponseBody") && exception.Data["ResponseBody"] is string responseBody)
            {
                return Task.FromResult(responseBody);
            }
            return Task.FromResult(exception.Message);
        }
    }
}
