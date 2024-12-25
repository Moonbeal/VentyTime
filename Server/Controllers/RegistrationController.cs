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
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(applicationUser, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(applicationUser, isPersistent: false);

                // Create associated User profile
                var userProfile = new ApplicationUser
                {
                    Id = applicationUser.Id,
                    UserName = applicationUser.UserName ?? model.Email,
                    Email = applicationUser.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                _context.Users.Add(userProfile);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(applicationUser);

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
                        Role = applicationUser.Role
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
                var token = GenerateJwtToken(user);
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
                        Role = user.Role
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

        private string GenerateJwtToken(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Role, UserRole.User.ToString()),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSecurityKey"] ?? string.Empty));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration["JwtExpiryInDays"] ?? "1"));

            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtAudience"],
                claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetEventRegistrations(int eventId)
        {
            try
            {
                var registrations = await _context.Registrations
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
        public async Task<ActionResult<IEnumerable<Registration>>> GetUserRegistrations(string userId)
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.Event)
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
        public async Task<ActionResult<Registration>> GetRegistration(int id)
        {
            try
            {
                var registration = await _context.Registrations
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
        public async Task<ActionResult<Registration>> RegisterForEvent(int eventId)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var @event = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                {
                    return NotFound(new { message = "Event not found" });
                }

                if (!@event.IsActive)
                {
                    return BadRequest(new { message = "Event is not active" });
                }

                if (@event.EndDate < DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Event has already ended" });
                }

                var confirmedRegistrations = @event.Registrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
                if (@event.MaxAttendees > 0 && confirmedRegistrations >= @event.MaxAttendees)
                {
                    return BadRequest(new { message = "Event is full" });
                }

                var existingRegistration = @event.Registrations?.FirstOrDefault(r => r.UserId == userId);
                if (existingRegistration != null)
                {
                    if (existingRegistration.Status == RegistrationStatus.Cancelled)
                    {
                        existingRegistration.Status = RegistrationStatus.Pending;
                        existingRegistration.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        return Ok(existingRegistration);
                    }
                    return BadRequest(new { message = "You are already registered for this event" });
                }

                var registration = new Registration
                {
                    EventId = eventId,
                    UserId = userId,
                    Status = RegistrationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRegistration), new { id = registration.Id }, registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while registering for event" });
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

                var registration = await _context.Registrations
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

                return Ok();
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
                var registration = await _context.Registrations
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
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == registration.EventId);

                if (@event == null)
                {
                    return NotFound(new { message = "Event not found" });
                }

                var confirmedRegistrations = @event.Registrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
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
