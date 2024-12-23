using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Set default culture to English
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Configure Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // Bind to localhost only
    serverOptions.Listen(System.Net.IPAddress.Loopback, 5241); // HTTP
    serverOptions.Listen(System.Net.IPAddress.Loopback, 7241, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Налаштування Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JwtSecurityKey"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        throw new InvalidOperationException("JWT Security Key is not configured");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtIssuer"],
        ValidAudience = builder.Configuration["JwtAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Додаємо контролери і конфігуруємо JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddRazorPages();

// Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.WithOrigins("https://localhost:7242")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
});

// Налаштування логування
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Налаштування HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Додаємо middleware для логування запитів
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "=== Incoming Request ===\n" +
        "Method: {Method}\n" +
        "Path: {Path}\n" +
        "QueryString: {Query}\n" +
        "ContentType: {ContentType}\n" +
        "Authorization: {Auth}",
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString,
        context.Request.ContentType,
        context.Request.Headers.Authorization.ToString());

    await next();

    logger.LogInformation(
        "=== Response ===\n" +
        "StatusCode: {StatusCode}",
        context.Response.StatusCode);
});

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapFallbackToFile("index.html");

// Логуємо всі зареєстровані маршрути
var endpointDataSource = app.Services
    .GetRequiredService<EndpointDataSource>();

app.Logger.LogInformation("=== Registered Routes ===");
foreach (var endpoint in endpointDataSource.Endpoints)
{
    if (endpoint is RouteEndpoint routeEndpoint)
    {
        var httpMethods = routeEndpoint.Metadata
            .GetOrderedMetadata<HttpMethodMetadata>()
            .SelectMany(m => m.HttpMethods)
            .DefaultIfEmpty("No HTTP methods");

        var auth = routeEndpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Any() ? "Authorized" : "Anonymous";

        app.Logger.LogInformation(
            "Endpoint: {DisplayName}\n" +
            "Route: {RoutePattern}\n" +
            "HTTP Methods: {HttpMethods}\n" +
            "Auth: {Auth}\n",
            routeEndpoint.DisplayName,
            routeEndpoint.RoutePattern.RawText,
            string.Join(", ", httpMethods),
            auth);
    }
}

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Ensuring database exists and is up to date...");
        await context.Database.EnsureCreatedAsync();

        // Ensure roles exist
        logger.LogInformation("Ensuring roles exist...");
        var roles = Enum.GetNames(typeof(UserRole));
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                logger.LogInformation("Creating role: {Role}", role);
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
        
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
        throw; // Re-throw to see the error in the console
    }
}

app.Run();
