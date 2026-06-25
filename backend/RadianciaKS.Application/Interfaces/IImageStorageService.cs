using Microsoft.AspNetCore.Http;

namespace RadianciaKS.Application.Interfaces
{
    public interface IImageStorageService
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
    }
}