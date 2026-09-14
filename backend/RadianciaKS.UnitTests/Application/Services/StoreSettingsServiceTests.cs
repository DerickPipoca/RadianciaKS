using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Domain.Models;
using Xunit;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class StoreSettingsServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<StoreSettingsRequestDto>> _validatorMock;
        private readonly Mock<IImageStorageService> _imageStorageServiceMock;
        private readonly StoreSettingsService _service;

        public StoreSettingsServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<StoreSettingsRequestDto>>();
            _imageStorageServiceMock = new Mock<IImageStorageService>();

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _service = new StoreSettingsService(
                _contextMock.Object,
                _validatorMock.Object,
                _imageStorageServiceMock.Object
            );
        }

        private void SetupStoreSettingsDbSet(List<StoreSettings> settings)
        {
            var mockDbSet = settings.BuildMockDbSet();
            _contextMock.Setup(c => c.StoreSettings).Returns(mockDbSet.Object);
        }

        private static StoreSettings CreateMockStoreSettings(Action<StoreSettings>? configure = null)
        {
            var settings = new StoreSettings
            {
                Id = Guid.NewGuid(),
                StoreName = "Restaurante Original",
                CNPJ = "12345678000199",
                Address = "Rua das Flores, 123",
                Phone = "44999999999",
                ReceiptFooter = "Obrigado pela preferência!",
                SmallLogoPath = "https://cdn.radiancia.com/logo-small.png",
                BigLogoPath = "https://cdn.radiancia.com/logo-big.png",
                ServiceCharge = 10.0m,
                ChargeServiceByDefault = true
            };

            configure?.Invoke(settings);
            return settings;
        }

        [Fact]
        public async Task GetSettings_Should_ReturnExistingSettings_When_Found()
        {
            var existing = CreateMockStoreSettings();
            SetupStoreSettingsDbSet([existing]);

            var result = await _service.GetSettings();

            result.ShouldNotBeNull();
            result.StoreName.ShouldBe("Restaurante Original");
            result.ServiceCharge.ShouldBe(10.0m);
            result.ChargeServiceByDefault.ShouldBeTrue();

            _contextMock.Verify(c => c.StoreSettings.Add(It.IsAny<StoreSettings>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task GetSettings_Should_CreateDefaultSettings_When_NoneExist()
        {
            SetupStoreSettingsDbSet([]);

            var result = await _service.GetSettings();

            result.ShouldNotBeNull();
            result.StoreName.ShouldBe("Novo Restaurante");

            _contextMock.Verify(c => c.StoreSettings.Add(It.Is<StoreSettings>(s => s.StoreName == "Novo Restaurante")), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateSettings_Should_UpdateAllProperties_And_SaveChanges_When_SettingsExist()
        {
            var existing = CreateMockStoreSettings();
            SetupStoreSettingsDbSet([existing]);

            var dto = new StoreSettingsRequestDto
            {
                StoreName = "Restaurante Atualizado",
                CNPJ = "98765432000188",
                Address = "Avenida Brasil, 500",
                Phone = "44988887777",
                ReceiptFooter = "Volte sempre!",
                SmallLogoPath = "https://cdn.radiancia.com/novo-small.png",
                BigLogoPath = "https://cdn.radiancia.com/novo-big.png",
                ServiceCharge = 12.5m,
                ChargeServiceByDefault = false
            };

            var result = await _service.UpdateSettings(dto);

            result.ShouldNotBeNull();
            existing.StoreName.ShouldBe("Restaurante Atualizado");
            existing.CNPJ.ShouldBe("98765432000188");
            existing.Address.ShouldBe("Avenida Brasil, 500");
            existing.Phone.ShouldBe("44988887777");
            existing.ReceiptFooter.ShouldBe("Volte sempre!");
            existing.SmallLogoPath.ShouldBe("https://cdn.radiancia.com/novo-small.png");
            existing.BigLogoPath.ShouldBe("https://cdn.radiancia.com/novo-big.png");
            existing.ServiceCharge.ShouldBe(12.5m);
            existing.ChargeServiceByDefault.ShouldBeFalse();

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateSettings_Should_ThrowArgumentException_When_SettingsDoNotExist()
        {
            SetupStoreSettingsDbSet([]);

            var dto = new StoreSettingsRequestDto { StoreName = "Loja Teste" };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.UpdateSettings(dto));

            exception.Message.ShouldContain("A loja informada não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UploadLogo_Should_UpdateSmallLogoPath_When_IsBigIsFalse()
        {
            var existing = CreateMockStoreSettings();
            SetupStoreSettingsDbSet([existing]);

            var fileMock = new Mock<IFormFile>();
            const string expectedUrl = "https://cdn.radiancia.com/logo_small/small-logo.webp";

            _imageStorageServiceMock
                .Setup(s => s.UploadImageAsync(fileMock.Object, "logo_small"))
                .ReturnsAsync(expectedUrl);

            var result = await _service.UploadLogo(fileMock.Object, isBig: false);

            result.ShouldBe(expectedUrl);
            existing.SmallLogoPath.ShouldBe(expectedUrl);

            _imageStorageServiceMock.Verify(s => s.UploadImageAsync(fileMock.Object, "logo_small"), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UploadLogo_Should_UpdateBigLogoPath_When_IsBigIsTrue()
        {
            var existing = CreateMockStoreSettings();
            SetupStoreSettingsDbSet([existing]);

            var fileMock = new Mock<IFormFile>();
            const string expectedUrl = "https://cdn.radiancia.com/logo_big/big-logo.webp";

            _imageStorageServiceMock
                .Setup(s => s.UploadImageAsync(fileMock.Object, "logo_big"))
                .ReturnsAsync(expectedUrl);

            var result = await _service.UploadLogo(fileMock.Object, isBig: true);

            result.ShouldBe(expectedUrl);
            existing.BigLogoPath.ShouldBe(expectedUrl);

            _imageStorageServiceMock.Verify(s => s.UploadImageAsync(fileMock.Object, "logo_big"), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UploadLogo_Should_ThrowArgumentException_When_SettingsDoNotExist()
        {
            SetupStoreSettingsDbSet([]);

            var fileMock = new Mock<IFormFile>();

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.UploadLogo(fileMock.Object, isBig: false));

            exception.Message.ShouldContain("A loja informada não existe.");
            _imageStorageServiceMock.Verify(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }
    }
}