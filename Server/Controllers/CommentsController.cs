using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(
            ICommentService commentService,
            ILogger<CommentsController> logger)
        {
            _commentService = commentService;
            _logger = logger;
        }

        [HttpGet("event/{eventId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByEvent(int eventId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByEventAsync(eventId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while retrieving comments" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Comment>>> GetCommentsByUser(string userId)
        {
            try
            {
                var comments = await _commentService.GetCommentsByUserAsync(userId);
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving comments" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetComment(int id)
        {
            try
            {
                var comment = await _commentService.GetCommentByIdAsync(id);
                if (comment == null)
                {
                    return NotFound();
                }
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {CommentId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving comment" });
            }
        }

        [HttpPost("event/{eventId}")]
        public async Task<ActionResult<Comment>> CreateComment(int eventId, [FromBody] Comment commentDto)
        {
            try
            {
                _logger.LogInformation("Received comment creation request for event {EventId}. Comment: {@Comment}", eventId, commentDto);

                if (commentDto == null)
                {
                    var message = "Comment cannot be null";
                    _logger.LogWarning("Comment is null for event {EventId}: {Message}", eventId, message);
                    return BadRequest(new { message });
                }

                if (string.IsNullOrWhiteSpace(commentDto.Content))
                {
                    var message = "Comment content cannot be empty";
                    _logger.LogWarning("Empty comment content for event {EventId}: {Message}", eventId, message);
                    return BadRequest(new { message });
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User ID from claims: {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    var message = "User must be authenticated to create comments";
                    _logger.LogWarning("Unauthorized comment creation attempt for event {EventId}: {Message}", eventId, message);
                    return Unauthorized(new { message });
                }

                var comment = new Comment
                {
                    EventId = eventId,
                    UserId = userId, 
                    Content = commentDto.Content.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsEdited = false,
                    UpdatedAt = null
                };

                _logger.LogInformation("Creating comment for event {EventId} by user {UserId}. Comment: {@Comment}", 
                    eventId, userId, comment);
                
                try 
                {
                    var createdComment = await _commentService.CreateCommentAsync(comment, userId);
                    _logger.LogInformation("Successfully created comment {CommentId} for event {EventId}", 
                        createdComment.Id, eventId);
                    
                    return CreatedAtAction(nameof(GetComment), new { id = createdComment.Id }, createdComment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error from comment service while creating comment for event {EventId}. Error: {Error}", 
                        eventId, ex.ToString());
                    throw;
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Event not found for comment creation. EventId: {EventId}", eventId);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid comment data for event {EventId}", eventId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for event {EventId}. Error: {Error}", eventId, ex.ToString());
                return StatusCode(500, new { message = "An error occurred while creating comment" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Comment>> UpdateComment(int id, [FromBody] Comment comment)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                comment.Id = id;
                var updatedComment = await _commentService.UpdateCommentAsync(comment, userId);
                return Ok(updatedComment);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", id);
                return StatusCode(500, new { message = "An error occurred while updating comment" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                await _commentService.DeleteCommentAsync(id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting comment" });
            }
        }
    }
}
