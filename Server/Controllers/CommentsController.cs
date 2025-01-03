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
        public async Task<ActionResult<IEnumerable<EventComment>>> GetCommentsByEvent(int eventId)
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
        public async Task<ActionResult<IEnumerable<EventComment>>> GetCommentsByUser(string userId)
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
        public async Task<ActionResult<EventComment>> GetComment(int id)
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
        public async Task<ActionResult<EventComment>> CreateComment(int eventId, [FromBody] EventComment comment)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                comment.EventId = eventId;
                var createdComment = await _commentService.CreateCommentAsync(comment, userId);
                return CreatedAtAction(nameof(GetComment), new { id = createdComment.Id }, createdComment);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while creating comment" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EventComment>> UpdateComment(int id, [FromBody] EventComment comment)
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
