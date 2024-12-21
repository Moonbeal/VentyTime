using System.Globalization;
using Microsoft.JSInterop;

namespace VentyTime.Client.Services
{
    public class CultureService
    {
        private readonly IJSRuntime _jsRuntime;

        public CultureService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeCultureAsync()
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            await _jsRuntime.InvokeVoidAsync("blazorCulture.set", "en-US");
        }
    }
}
