using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VentyTime.Client;
using MudBlazor.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using VentyTime.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using VentyTime.Client.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for all endpoints
builder.Services.AddHttpClient("VentyTime.ServerAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Add NoAuth HttpClient for registration/login
builder.Services.AddHttpClient("VentyTime.ServerAPI.NoAuth", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add custom message handler for auth
builder.Services.AddScoped<AuthenticationHeaderHandler>();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add MudBlazor
builder.Services.AddMudServices();

// Add LocalStorage
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

// Add Auth State Provider
builder.Services.AddScoped<AuthenticationStateProvider, VentyTime.Client.Auth.CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// Add Services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IAuthService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var logger = sp.GetRequiredService<ILogger<AuthService>>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    
    return new AuthService(
        httpClientFactory,
        authStateProvider,
        localStorage,
        logger,
        navigationManager
    );
});

builder.Services.AddScoped<IUserService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("VentyTime.ServerAPI");
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var authStateProvider = sp.GetRequiredService<AuthenticationStateProvider>();
    var snackbar = sp.GetRequiredService<ISnackbar>();
    var logger = sp.GetRequiredService<ILogger<UserService>>();
    
    return new UserService(
        httpClient,
        localStorage,
        authStateProvider,
        snackbar,
        logger
    );
});

builder.Services.AddScoped<IRegistrationService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("VentyTime.ServerAPI");
    var snackbar = sp.GetRequiredService<ISnackbar>();
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var authService = sp.GetRequiredService<IAuthService>();
    return new RegistrationService(httpClient, snackbar, localStorage, authService);
});

builder.Services.AddScoped<ICommentService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("VentyTime.ServerAPI");
    return new CommentService(httpClient);
});

builder.Services.AddScoped<CultureService>();

await builder.Build().RunAsync();
