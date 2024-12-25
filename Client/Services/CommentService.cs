using System.Net.Http.Json;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface ICommentService
    {
        Task<List<Comment>> GetEventCommentsAsync(int eventId);
        Task<bool> AddCommentAsync(Comment comment);
        Task<bool> UpdateCommentAsync(Comment comment);
        Task<bool> DeleteCommentAsync(int commentId);
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

        public async Task<bool> AddCommentAsync(Comment comment)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/comments", comment);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCommentAsync(Comment comment)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/comments/{comment.Id}", comment);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/comments/{commentId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
