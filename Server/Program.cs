using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using VentyTime.Server.Data;
using VentyTime.Server.Models;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Enable detailed errors in development
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseSetting("DetailedErrors", "true");
}

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
    });

// Configure database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(Console.WriteLine, LogLevel.Information);
});

// Register ImageService
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5242880; // 5MB
});

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 5242880; // 5MB
});

builder.Services.AddRazorPages();

// Add Identity services
builder.Services.AddIdentity<VentyTime.Shared.Models.ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
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
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var claims = context.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
            logger.LogInformation("Token validated. Claims: {Claims}", string.Join(", ", claims ?? Array.Empty<string>()));
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Authentication failed");
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Authentication challenge. Error: {Error}", context.Error);
            return Task.CompletedTask;
        }
    };
});

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // This is important - we want to allow anonymous access by default
    options.FallbackPolicy = null;

    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("RequireOrganizerRole", policy =>
        policy.RequireRole("Admin", "Organizer"));

    options.AddPolicy("RequireUserRole", policy =>
        policy.RequireRole("Admin", "Organizer", "User"));
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "https://localhost:7242",
                "http://localhost:5242",
                "https://localhost:7241",
                "http://localhost:5241",
                "https://localhost:5001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<VentyTime.Shared.Models.ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Running database migrations...");
        await context.Database.MigrateAsync();

        logger.LogInformation("Ensuring roles are seeded...");
        await SeedData.EnsureRolesAsync(roleManager);

        // Verify roles were created
        foreach (var roleName in Enum.GetNames(typeof(UserRole)).Where(r => r != "None"))
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            logger.LogInformation("Role {RoleName} exists: {Exists}", roleName, roleExists);
        }

        logger.LogInformation("Seeding events and test users...");
        await SeedData.SeedEventsAsync(context, userManager);

        // Ensure roles exist
        var roles = new[] { "Admin", "Organizer", "User" };
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
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw; // Rethrow to prevent the application from starting with an improperly initialized database
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Ensure wwwroot/uploads directory exists
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseHttpsRedirection();

// Configure development-specific middleware
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/_framework/aspnetcore-browser-refresh.js")
    {
        context.Response.ContentType = "application/javascript";
        await context.Response.WriteAsync(@"// <![CDATA[
            (function() {
                const eventSource = new EventSource('/_framework/aspnetcore-browser-refresh');
                eventSource.onmessage = function(event) {
                    if (event.data === 'reload') {
                        window.location.reload();
                    }
                };
            })();
        // ]]>//");
        return;
    }
    await next();
});

// Add detailed error handling in development
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred: {Error}", ex.ToString());
        
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            
            var error = new
            {
                message = "An error occurred while processing your request",
                error = ex.Message,
                stackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null,
                innerError = ex.InnerException?.Message
            };
            
            await context.Response.WriteAsJsonAsync(error);
        }
    }
});

app.UseBlazorFrameworkFiles();

// Configure static files
app.UseStaticFiles(); // Serve files from wwwroot

// Create and configure images directory
var imagesPath = Path.Combine(builder.Environment.WebRootPath, "images", "events");
if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
}

// Serve files from the images directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "images")),
    RequestPath = "/images"
});

// Serve files from the uploads directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "uploads")),
    RequestPath = "/uploads"
});

// Configure CORS
app.UseCors(policy =>
    policy.WithOrigins(
        "https://localhost:7241",
        "http://localhost:5241",
        "https://localhost:7242",
        "http://localhost:5242")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials());

// Ensure wwwroot exists
if (!Directory.Exists(builder.Environment.WebRootPath))
{
    Directory.CreateDirectory(builder.Environment.WebRootPath);
}

// Create thumbnails directory if it doesn't exist
var thumbnailsDirectory = Path.Combine(builder.Environment.WebRootPath, "thumbnails");
if (!Directory.Exists(thumbnailsDirectory))
{
    Directory.CreateDirectory(thumbnailsDirectory);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
