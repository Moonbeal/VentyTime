using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Blazored.LocalStorage;
using VentyTime.Client;
using VentyTime.Client.Services;
using VentyTime.Client.Theme;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add core services
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddLogging(logging => 
{
    logging.SetMinimumLevel(LogLevel.Information);
});

// Register auth services
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Register other services
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure HttpClient with custom auth handler
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("VentyTime.ServerAPI", client => 
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
})
.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

builder.Services.AddHttpClient("VentyTime.ServerAPI.NoAuth", client => 
{
    client.BaseAddress = new Uri("https://localhost:7241");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
});

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("VentyTime.ServerAPI"));

// Add custom services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

await builder.Build().RunAsync();
