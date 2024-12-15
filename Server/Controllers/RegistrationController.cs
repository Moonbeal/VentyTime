using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public RegistrationController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrations()
    {
        return await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Registration>> GetRegistration(int id)
    {
        var registration = await _context.Registrations
            .Include(r => r.Event)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (registration == null)
        {
            return NotFound();
        }

        return registration;
    }

    [HttpPost]
    public async Task<ActionResult<Registration>> CreateRegistration(Registration registration)
    {
        var existingRegistration = await _context.Registrations
            .FirstOrDefaultAsync(r => r.EventId == registration.EventId && r.UserId == registration.UserId);

        if (existingRegistration != null)
        {
            return BadRequest("User is already registered for this event.");
        }

        var @event = await _context.Events.FindAsync(registration.EventId);
        if (@event == null)
        {
            return BadRequest("Event not found.");
        }

        var currentRegistrations = await _context.Registrations
            .CountAsync(r => r.EventId == registration.EventId && !r.CancelledAt.HasValue);

        if (currentRegistrations >= @event.MaxAttendees)
        {
            return BadRequest("Event is already full.");
        }

        _context.Registrations.Add(registration);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRegistration), new { id = registration.Id }, registration);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelRegistration(int id)
    {
        var registration = await _context.Registrations.FindAsync(id);

        if (registration == null)
        {
            return NotFound();
        }

        registration.CancelledAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
