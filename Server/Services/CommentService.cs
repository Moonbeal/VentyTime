using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface ICommentService
    {
        Task<Comment> CreateCommentAsync(Comment comment, string userId);
        Task<Comment> UpdateCommentAsync(Comment comment, string userId);
        Task<bool> DeleteCommentAsync(int id, string userId);
        Task<Comment?> GetCommentByIdAsync(int id);
        Task<IEnumerable<Comment>> GetCommentsByEventAsync(int eventId);
        Task<IEnumerable<Comment>> GetCommentsByUserAsync(string userId);
    }

    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentService> _logger;
        private readonly IEventService _eventService;

        public CommentService(
            ApplicationDbContext context,
            ILogger<CommentService> logger,
            IEventService eventService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        public async Task<Comment> CreateCommentAsync(Comment comment, string userId)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(comment.Content)) throw new ArgumentException("Comment content cannot be empty");
            if (comment.Content.Length > 1000) throw new ArgumentException("Comment content cannot exceed 1000 characters");

            try
            {
                _logger.LogInformation("Creating comment for event {EventId} by user {UserId}", 
                    comment.EventId, userId);

                var @event = await _eventService.GetEventByIdAsync(comment.EventId) 
                    ?? throw new KeyNotFoundException($"Event with ID {comment.EventId} not found");

                if (!@event.IsActive)
                {
                    throw new InvalidOperationException("Cannot comment on inactive events");
                }

                comment.UserId = userId;
                comment.CreatedAt = DateTime.UtcNow;
                comment.IsEdited = false;
                comment.UpdatedAt = null;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Comments.Add(comment);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully created comment {CommentId} for event {EventId}", 
                        comment.Id, comment.EventId);

                    return comment;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error saving comment to database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                throw;
            }
        }

        public async Task<Comment> UpdateCommentAsync(Comment comment, string userId)
        {
            if (comment == null) throw new ArgumentNullException(nameof(comment));
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(comment.Content)) throw new ArgumentException("Comment content cannot be empty");
            if (comment.Content.Length > 1000) throw new ArgumentException("Comment content cannot exceed 1000 characters");

            try
            {
                _logger.LogInformation("Updating comment {CommentId} by user {UserId}", 
                    comment.Id, userId);

                var existingComment = await _context.Comments
                    .Include(c => c.Event)
                    .FirstOrDefaultAsync(c => c.Id == comment.Id) 
                    ?? throw new KeyNotFoundException($"Comment with ID {comment.Id} not found");

                if (existingComment.Event == null)
                {
                    throw new InvalidOperationException("Comment's event is not found");
                }

                if (!existingComment.Event.IsActive)
                {
                    throw new InvalidOperationException("Cannot update comments on inactive events");
                }

                if (existingComment.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this comment");
                }

                existingComment.Content = comment.Content;
                existingComment.IsEdited = true;
                existingComment.UpdatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully updated comment {CommentId}", comment.Id);

                    return existingComment;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating comment in database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment");
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(int id, string userId)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID cannot be empty", nameof(userId));

            try
            {
                _logger.LogInformation("Deleting comment {CommentId} by user {UserId}", id, userId);

                var comment = await _context.Comments
                    .Include(c => c.Event)
                    .FirstOrDefaultAsync(c => c.Id == id) 
                    ?? throw new KeyNotFoundException($"Comment with ID {id} not found");

                if (comment.Event == null)
                {
                    throw new InvalidOperationException("Comment's event is not found");
                }

                if (!comment.Event.IsActive)
                {
                    throw new InvalidOperationException("Cannot delete comments on inactive events");
                }

                if (comment.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this comment");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Comments.Remove(comment);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully deleted comment {CommentId}", id);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting comment from database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment");
                throw;
            }
        }

        public async Task<Comment?> GetCommentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting comment {CommentId}", id);

                return await _context.Comments
                    .Include(c => c.Event)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {CommentId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Comment>> GetCommentsByEventAsync(int eventId)
        {
            try
            {
                _logger.LogInformation("Getting comments for event {EventId}", eventId);

                return await _context.Comments
                    .Include(c => c.User)
                    .Where(c => c.EventId == eventId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for event {EventId}", eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Comment>> GetCommentsByUserAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting comments for user {UserId}", userId);

                return await _context.Comments
                    .Include(c => c.Event)
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for user {UserId}", userId);
                throw;
            }
        }
    }
}
