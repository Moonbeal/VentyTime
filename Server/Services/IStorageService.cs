using System;
using System.IO;
using System.Threading.Tasks;

namespace VentyTime.Server.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileName);
    }
}
