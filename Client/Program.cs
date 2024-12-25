using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VentyTime.Client;
using VentyTime.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using MudBlazor;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for auth endpoints
builder.Services.AddHttpClient("VentyTime.ServerAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Configure HttpClient for non-auth endpoints
builder.Services.AddHttpClient("VentyTime.ServerAPI.NoAuth", client =>
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Add MudBlazor
builder.Services.AddMudServices();

// Add LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add Auth State Provider
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();

// Add Services
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

builder.Services.AddScoped<IEventService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("VentyTime.ServerAPI");
    return new EventService(httpClient);
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
