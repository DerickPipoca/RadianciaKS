using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services
{
    public class StoreSettingsService : IStoreSettingsService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<StoreSettingsRequestDto> _validator;
        private readonly IImageStorageService _imageStorageService;
        private readonly StoreSettingsMapper _mapper;

        public StoreSettingsService(IApplicationDbContext applicationDbContext, IValidator<StoreSettingsRequestDto> validator, IImageStorageService imageStorageService)
        {
            _context = applicationDbContext;
            _validator = validator;
            _imageStorageService = imageStorageService;
            _mapper = new StoreSettingsMapper();
        }

        public async Task<StoreSettingsResponseDto> GetSettings()
        {
            var settings = await _context.StoreSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new StoreSettings { StoreName = "Novo Restaurante" };
                _context.StoreSettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return _mapper.ToDto(settings);
        }

        public async Task<StoreSettingsResponseDto> UpdateSettings(StoreSettingsRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var storeSettings = await _context.StoreSettings.FirstOrDefaultAsync();

            if (storeSettings == null)
                throw new ArgumentException("A loja informada não existe.");

            storeSettings.Update(
                dto.StoreName,
                dto.CNPJ,
                dto.Address,
                dto.Phone,
                dto.ReceiptFooter,
                dto.SmallLogoPath,
                dto.BigLogoPath,
                dto.ServiceCharge
            );

            await _context.SaveChangesAsync();
            return _mapper.ToDto(storeSettings);
        }

        public async Task<string> UploadLogo(IFormFile file, bool isBig)
        {
            var storeSettings = await _context.StoreSettings.FirstOrDefaultAsync();
            if (storeSettings == null)
                throw new ArgumentException("A loja informada não existe.");

            var suffix = isBig ? "big" : "small";

            var imageUrl = await _imageStorageService.UploadImageAsync(file, $"logo_{suffix}");

            if (!isBig)
                storeSettings.SmallLogoPath = imageUrl;
            else
                storeSettings.BigLogoPath = imageUrl;

            await _context.SaveChangesAsync();

            return imageUrl;
        }
    }
}