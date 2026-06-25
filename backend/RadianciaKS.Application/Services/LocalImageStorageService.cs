using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class LocalImageStorageService : IImageStorageService
    {
        private readonly IWebHostEnvironment _env;

        public LocalImageStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Nenhum arquivo enviado.");

            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName.Replace(" ", "-");
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}