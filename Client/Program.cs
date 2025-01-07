using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VentyTime.Client;
using VentyTime.Client.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CultureService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add HTTP client with auth header handler
builder.Services.AddScoped<AuthenticationHeaderHandler>();

// Get the API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5242";

// Client with auth header handler
builder.Services.AddHttpClient("VentyTime.ServerAPI", 
    client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticationHeaderHandler>();

// Client without auth header handler for login/register
builder.Services.AddHttpClient("VentyTime.ServerAPI.NoAuth", 
    client => client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddScoped(sp => 
    sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("VentyTime.ServerAPI"));

builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
