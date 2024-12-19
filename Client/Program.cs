using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VentyTime.Client;
using VentyTime.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Register logging services first
builder.Services.AddLogging();

// Register Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Register HTTP clients
builder.Services.AddScoped(sp => 
{
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    return client;
});

// Register MudBlazor services
builder.Services.AddMudServices();

// Register Authentication services in correct order
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register other services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();

await builder.Build().RunAsync();
