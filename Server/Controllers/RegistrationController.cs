using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

        public RegistrationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
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
            return await _context.Registrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetUserRegistrations(string userId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        [Authorize]
        [HttpPost("event/{eventId}")]
        public async Task<IActionResult> RegisterForEvent(int eventId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null)
            {
                return NotFound("Event not found");
            }

            if (@event.IsFull)
            {
                return BadRequest("Event is full");
            }

            if (@event.Registrations.Any(r => r.UserId == userId))
            {
                return BadRequest("Already registered for this event");
            }

            var registration = new Registration
            {
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpDelete("{registrationId}")]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (registration == null)
            {
                return NotFound("Registration not found");
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
