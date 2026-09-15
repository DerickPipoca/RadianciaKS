using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.Services;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class LocalImageStorageServiceTests : IDisposable
    {
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly string _tempWebRoot;

        public LocalImageStorageServiceTests()
        {
            _envMock = new Mock<IWebHostEnvironment>();
            _tempWebRoot = Path.Combine(Path.GetTempPath(), $"webroot_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempWebRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempWebRoot))
            {
                try
                {
                    Directory.Delete(_tempWebRoot, recursive: true);
                }
                catch
                {
                    // Ignora bloqueios temporários de exclusão pelo SO
                }
            }
        }

        private static IFormFile CreateFormFile(string fileName, string content = "fake image content")
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName);
        }

        [Fact]
        public async Task UploadImageAsync_Should_ThrowArgumentException_When_FileIsNull()
        {
            var service = new LocalImageStorageService(_envMock.Object);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                service.UploadImageAsync(null!, "banners"));

            exception.Message.ShouldBe("Nenhum arquivo enviado.");
        }

        [Fact]
        public async Task UploadImageAsync_Should_ThrowArgumentException_When_FileIsEmpty()
        {
            var emptyFile = new FormFile(new MemoryStream(Array.Empty<byte>()), 0, 0, "file", "empty.png");
            var service = new LocalImageStorageService(_envMock.Object);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                service.UploadImageAsync(emptyFile, "banners"));

            exception.Message.ShouldBe("Nenhum arquivo enviado.");
        }

        [Fact]
        public async Task UploadImageAsync_Should_SaveFile_ReplaceSpaces_And_ReturnRelativeUrl()
        {
            _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);
            var service = new LocalImageStorageService(_envMock.Object);

            var file = CreateFormFile("minha foto especial.png", "conteudo do arquivo binario");
            var folderName = "produtos";

            var resultUrl = await service.UploadImageAsync(file, folderName);

            resultUrl.ShouldStartWith($"/uploads/{folderName}/");
            resultUrl.ShouldContain("minha-foto-especial.png");

            var expectedFolder = Path.Combine(_tempWebRoot, "uploads", folderName);
            Directory.Exists(expectedFolder).ShouldBeTrue();

            var savedFiles = Directory.GetFiles(expectedFolder);
            savedFiles.Length.ShouldBe(1);
            savedFiles[0].ShouldContain("minha-foto-especial.png");

            var fileContent = await File.ReadAllTextAsync(savedFiles[0]);
            fileContent.ShouldBe("conteudo do arquivo binario");
        }

        [Fact]
        public async Task UploadImageAsync_Should_FallbackToCurrentDirectory_When_WebRootPathIsNull()
        {
            _envMock.Setup(e => e.WebRootPath).Returns((string)null!);

            var service = new LocalImageStorageService(_envMock.Object);
            var folderName = $"fallback_test_{Guid.NewGuid():N}";
            var file = CreateFormFile("fallback.jpg");

            var expectedFallbackPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folderName);

            try
            {
                var resultUrl = await service.UploadImageAsync(file, folderName);

                resultUrl.ShouldBe($"/uploads/{folderName}/" + Path.GetFileName(Directory.GetFiles(expectedFallbackPath).First()));
                Directory.Exists(expectedFallbackPath).ShouldBeTrue();
            }
            finally
            {
                if (Directory.Exists(expectedFallbackPath))
                {
                    Directory.Delete(expectedFallbackPath, recursive: true);
                }
            }
        }
    }
}