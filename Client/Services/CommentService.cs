using System.Net.Http.Json;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface ICommentService
    {
        Task<List<Comment>> GetEventCommentsAsync(int eventId);
        Task<HttpResponseMessage> AddCommentAsync(int eventId, Comment comment);
        Task<HttpResponseMessage> UpdateCommentAsync(Comment comment);
        Task<HttpResponseMessage> DeleteCommentAsync(int commentId);
    }

    public class CommentService : ICommentService
    {
        private readonly HttpClient _httpClient;

        public CommentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Comment>> GetEventCommentsAsync(int eventId)
        {
            return await _httpClient.GetFromJsonAsync<List<Comment>>($"api/comments/event/{eventId}") ?? new List<Comment>();
        }

        public async Task<HttpResponseMessage> AddCommentAsync(int eventId, Comment comment)
        {
            try
            {
                Console.WriteLine($"Sending comment to server: EventId={eventId}, UserId={comment.UserId}, Content={comment.Content}");
                var response = await _httpClient.PostAsJsonAsync($"api/comments/event/{eventId}", comment);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server error response: {error}");
                }
                
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in AddCommentAsync: {ex}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> UpdateCommentAsync(Comment comment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/comments/{comment.Id}", comment);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateCommentAsync: {ex}");
                throw;
            }
        }

        public async Task<HttpResponseMessage> DeleteCommentAsync(int commentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/comments/{commentId}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in DeleteCommentAsync: {ex}");
                throw;
            }
        }
    }
}
