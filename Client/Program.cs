using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VentyTime.Client;
using VentyTime.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using System.Globalization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure localization services
builder.Services.AddLocalization();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register logging services first
builder.Services.AddLogging();

// Register Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Configure HttpClient with auth handler
builder.Services.AddScoped<CustomAuthorizationMessageHandler>();

// Configure the HttpClient for the API
builder.Services.AddHttpClient("VentyTime.ServerAPI", client => 
{
    client.BaseAddress = new Uri("https://localhost:7241");
})
.AddHttpMessageHandler<CustomAuthorizationMessageHandler>();

// Register HttpClient factory
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
    .CreateClient("VentyTime.ServerAPI"));

// Register MudBlazor services
builder.Services.AddMudServices();

// Register authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register other services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<CultureService>();

await builder.Build().RunAsync();
