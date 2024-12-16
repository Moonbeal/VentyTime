using System.Threading.Tasks;

namespace VentyTime.Client.Services
{
    public interface IAuthService
    {
        Task<string> GetUserId();
        Task<string> GetUsername();
        Task<bool> IsAuthenticated();
        Task<string> GetToken();
    }
}
