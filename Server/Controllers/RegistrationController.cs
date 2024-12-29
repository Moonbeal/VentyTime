using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<RegistrationController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new AuthResponse { Success = false, Message = "Email already registered" });

            var applicationUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(applicationUser, model.Password);

            if (result.Succeeded)
            {
                // Add user to the default User role
                await _userManager.AddToRoleAsync(applicationUser, UserRole.User.ToString());
                await _signInManager.SignInAsync(applicationUser, isPersistent: false);

                var token = await GenerateJwtToken(applicationUser);

                // Get the user's role
                var roles = await _userManager.GetRolesAsync(applicationUser);
                var role = roles.FirstOrDefault();
                var userRole = Enum.TryParse<UserRole>(role, out var parsedRole) ? parsedRole : UserRole.User;

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    Token = token,
                    User = new UserDto
                    {
                        Id = applicationUser.Id,
                        Email = applicationUser.Email!,
                        FirstName = applicationUser.FirstName,
                        LastName = applicationUser.LastName,
                        AvatarUrl = applicationUser.AvatarUrl,
                        Role = userRole
                    }
                });
            }

            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Registration failed",
                Token = string.Empty,
                User = new UserDto
                {
                    Id = string.Empty,
                    Email = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    AvatarUrl = string.Empty,
                    Role = UserRole.User
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Token = string.Empty,
                    User = new UserDto()
                });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var token = await GenerateJwtToken(user);
                return Ok(new AuthResponse
                {
                    Success = true,
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AvatarUrl = user.AvatarUrl,
                        Role = Enum.TryParse<UserRole>((await _userManager.GetRolesAsync(user)).FirstOrDefault(), out var parsedRole) ? parsedRole : UserRole.User
                    }
                });
            }

            return BadRequest(new AuthResponse
            {
                Success = false,
                Message = "Invalid email or password",
                Token = string.Empty,
                User = new UserDto
                {
                    Id = string.Empty,
                    Email = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    AvatarUrl = string.Empty,
                    Role = UserRole.User
                }
            });
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? UserRole.User.ToString();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration["JwtSettings:ExpirationInDays"] ?? "7"));

            var token = new JwtSecurityToken(
                _configuration["JwtSettings:Issuer"],
                _configuration["JwtSettings:Audience"],
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<EventRegistration>>> GetEventRegistrations(int eventId)
        {
            try
            {
                var registrations = await _context.EventRegistrations
                    .Include(r => r.Event)
                    .Include(r => r.User)
                    .Where(r => r.EventId == eventId)
                    .ToListAsync();

                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registrations for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while retrieving registrations" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<EventRegistration>>> GetUserRegistrations(string userId)
        {
            try
            {
                var registrations = await _context.EventRegistrations
                    .Include(r => r.Event)
                    .Include(r => r.User)
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registrations for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving registrations" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventRegistration>> GetRegistration(int id)
        {
            try
            {
                var registration = await _context.EventRegistrations
                    .Include(r => r.Event)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (registration == null)
                {
                    return NotFound();
                }

                return Ok(registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving registration" });
            }
        }

        [Authorize]
        [HttpPost("event/{eventId}")]
        public async Task<ActionResult<EventRegistration>> RegisterForEvent(int eventId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized registration attempt for event {EventId}", eventId);
                    return Unauthorized(new { message = "You must be logged in to register for events." });
                }

                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User {UserId} not found while registering for event {EventId}", userId, eventId);
                    return BadRequest(new { message = "User not found" });
                }

                // Load event with its registrations
                var @event = await _context.Events
                    .Include(e => e.EventRegistrations)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                {
                    _logger.LogWarning("Attempted to register for non-existent event {EventId}", eventId);
                    return NotFound(new { message = "Event not found" });
                }

                if (!@event.IsActive)
                {
                    _logger.LogWarning("Attempted to register for inactive event {EventId}", eventId);
                    return BadRequest(new { message = "This event is not active" });
                }

                if (@event.EndDate < DateTime.UtcNow)
                {
                    _logger.LogWarning("Attempted to register for ended event {EventId}", eventId);
                    return BadRequest(new { message = "This event has already ended" });
                }

                if (@event.StartDate < DateTime.UtcNow)
                {
                    _logger.LogWarning("Attempted to register for started event {EventId}", eventId);
                    return BadRequest(new { message = "This event has already started" });
                }

                // Check current capacity
                var confirmedRegistrations = @event.EventRegistrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
                if (@event.MaxAttendees > 0 && confirmedRegistrations >= @event.MaxAttendees)
                {
                    _logger.LogWarning("Attempted to register for full event {EventId}", eventId);
                    return BadRequest(new { message = "This event is full" });
                }

                // Check for existing registration using a direct query
                var existingRegistration = @event.EventRegistrations?.FirstOrDefault(r => r.UserId == userId);

                if (existingRegistration != null)
                {
                    if (existingRegistration.Status == RegistrationStatus.Cancelled)
                    {
                        existingRegistration.Status = RegistrationStatus.Pending;
                        existingRegistration.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Reactivated cancelled registration for user {UserId} in event {EventId}", userId, eventId);
                        
                        // Load and return the full registration details
                        var reactivatedRegistration = await _context.EventRegistrations
                            .Include(r => r.Event)
                            .Include(r => r.User)
                            .FirstOrDefaultAsync(r => r.Id == existingRegistration.Id);
                            
                        return Ok(reactivatedRegistration);
                    }
                    
                    _logger.LogWarning("User {UserId} attempted to register again for event {EventId}", userId, eventId);
                    return BadRequest(new { message = "You are already registered for this event" });
                }

                // Create new registration
                var registration = new EventRegistration
                {
                    EventId = eventId,
                    UserId = userId,
                    Status = RegistrationStatus.Pending,
                    RegistrationDate = DateTime.UtcNow
                };

                _context.EventRegistrations.Add(registration);

                // Update event capacity
                @event.CurrentCapacity = confirmedRegistrations + 1;
                _context.Events.Update(@event);

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("User {UserId} successfully registered for event {EventId}", userId, eventId);
                
                // Load and return the full registration details
                var savedRegistration = await _context.EventRegistrations
                    .Include(r => r.Event)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == registration.Id);

                if (savedRegistration == null)
                {
                    _logger.LogError("Failed to load saved registration {RegistrationId} for event {EventId}", registration.Id, eventId);
                    return StatusCode(500, new { message = "Registration was created but could not be loaded" });
                }
                    
                return Ok(savedRegistration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user for event {EventId}. Error: {Error}", eventId, ex.ToString());
                return StatusCode(500, new { message = "An error occurred while registering for the event", error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registration = await _context.EventRegistrations
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (registration == null)
                {
                    return NotFound();
                }

                if (registration.UserId != userId)
                {
                    return Forbid();
                }

                if (registration.Status == RegistrationStatus.Cancelled)
                {
                    return BadRequest(new { message = "Registration is already cancelled" });
                }

                registration.Status = RegistrationStatus.Cancelled;
                registration.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling registration" });
            }
        }

        [Authorize(Roles = "Admin,Organizer")]
        [HttpPost("{id}/confirm")]
        public async Task<IActionResult> ConfirmRegistration(int id)
        {
            try
            {
                var registration = await _context.EventRegistrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (registration == null)
                {
                    return NotFound();
                }

                if (registration.Status != RegistrationStatus.Pending)
                {
                    return BadRequest(new { message = "Registration cannot be confirmed" });
                }

                var @event = await _context.Events
                    .Include(e => e.EventRegistrations)
                    .FirstOrDefaultAsync(e => e.Id == registration.EventId);

                if (@event == null)
                {
                    return NotFound(new { message = "Event not found" });
                }

                var confirmedRegistrations = @event.EventRegistrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
                if (@event.MaxAttendees > 0 && confirmedRegistrations >= @event.MaxAttendees)
                {
                    return BadRequest(new { message = "Event is full" });
                }

                registration.Status = RegistrationStatus.Confirmed;
                registration.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while confirming registration" });
            }
        }
    }
}
