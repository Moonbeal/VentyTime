using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace VentyTime.Client.Services;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthenticationHeaderHandler> _logger;

    public AuthenticationHeaderHandler(
        ILocalStorageService localStorage,
        ILogger<AuthenticationHeaderHandler> logger)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No auth token found in local storage");
                return await base.SendAsync(request, cancellationToken);
            }

            _logger.LogInformation("Adding auth token to request: {TokenPrefix}...", 
                token[..Math.Min(10, token.Length)]);
                
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            
            var response = await base.SendAsync(request, cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Received unauthorized response, clearing auth token");
                await _localStorage.RemoveItemAsync("authToken", cancellationToken);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in authentication handler");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
