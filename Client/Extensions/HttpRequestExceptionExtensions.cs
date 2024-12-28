using System.Net.Http;
using System.Threading.Tasks;

namespace VentyTime.Client.Extensions
{
    public static class HttpRequestExceptionExtensions
    {
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
