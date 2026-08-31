using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<PromotionRequestDto> _validator;
        private readonly PromotionMapper _mapper;
        private readonly ProductMapper _productMapper;

        public PromotionService(IApplicationDbContext context, IValidator<PromotionRequestDto> validator)
        {
            _context = context;
            _validator = validator;
            _productMapper = new ProductMapper();
            _mapper = new PromotionMapper();
        }

        public async Task<PromotionResponseDto> CreatePromotion(PromotionRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var promotion = _mapper.ToEntity(dto);
            promotion.Running = true;

            promotion.PromotionModifiers = dto.PromotionModifiers.Select(mod => new PromotionModifier
            {
                ModifierOptionId = mod.ModifierOptionId,
                OverridePrice = mod.OverridePrice,
                PromotionId = promotion.Id
            }).ToList();

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            return await GetPromotionById(promotion.Id);
        }

        public async Task<IEnumerable<PromotionResponseDto>> GetActivePromotions()
        {
            var promotions = await _context.Promotions
                .Where(p => p.Running)
                .Include(p => p.BaseProduct)
                    .ThenInclude(bp => bp.Category)
                .Include(p => p.BaseProduct)
                    .ThenInclude(bp => bp.ModifierGroups)
                        .ThenInclude(mg => mg.Options)
                .Include(p => p.PromotionModifiers)
                .ToListAsync();

            var result = new List<PromotionResponseDto>();

            foreach (var promo in promotions)
            {
                var dto = new PromotionResponseDto
                {
                    Id = promo.Id,
                    Name = promo.Name,
                    Description = promo.Description,
                    BaseProduct = _productMapper.ToDto(promo.BaseProduct)
                };

                if (promo.PromotionalPrice.HasValue)
                {
                    dto.BaseProduct.PromotionalPrice = promo.PromotionalPrice.Value;
                    dto.BaseProduct.IsPromotional = true;
                }

                foreach (var group in dto.BaseProduct.ModifierGroups)
                {
                    foreach (var option in group.Options)
                    {
                        var overrideRule = promo.PromotionModifiers
                            .FirstOrDefault(pm => pm.ModifierOptionId == option.Id);

                        if (overrideRule != null)
                        {
                            option.PromotionalPrice = overrideRule.OverridePrice;
                            option.IsPromotional = true;
                        }
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<PagedResponse<PromotionResponseDto>> GetAllPromotions(ProductQueryParameters queryParameters)
        {
            var promotions = await _context.Promotions
                .Include(p => p.BaseProduct)
                    .ThenInclude(bp => bp.Category)
                .Include(p => p.PromotionModifiers)
                .ToListAsync();

            var result = promotions.Select(promo => new PromotionResponseDto
            {
                Id = promo.Id,
                Name = promo.Name,
                Description = promo.Description,
                BaseProduct = _productMapper.ToDto(promo.BaseProduct)
            }).ToList();

            var pagedResponse = new PagedResponse<PromotionResponseDto>
            {
                Data = result,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                TotalRecords = promotions.Count
            };

            return pagedResponse;
        }

        public async Task<PromotionResponseDto> GetPromotionById(Guid id)
        {
            var promo = await _context.Promotions
                .Include(p => p.BaseProduct)
                    .ThenInclude(bp => bp.Category)
                .Include(p => p.BaseProduct)
                    .ThenInclude(bp => bp.ModifierGroups)
                        .ThenInclude(mg => mg.Options)
                .Include(p => p.PromotionModifiers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promo == null)
                throw new ArgumentException("Promoção não encontrada.");

            var dto = new PromotionResponseDto
            {
                Id = promo.Id,
                Name = promo.Name,
                Description = promo.Description,
                BaseProduct = _productMapper.ToDto(promo.BaseProduct)
            };

            if (promo.PromotionalPrice.HasValue)
            {
                dto.BaseProduct.PromotionalPrice = promo.PromotionalPrice.Value;
                dto.BaseProduct.IsPromotional = true;
            }

            foreach (var group in dto.BaseProduct.ModifierGroups)
            {
                foreach (var option in group.Options)
                {
                    var overrideRule = promo.PromotionModifiers
                        .FirstOrDefault(pm => pm.ModifierOptionId == option.Id);

                    if (overrideRule != null)
                    {
                        option.PromotionalPrice = overrideRule.OverridePrice;
                        option.IsPromotional = true;
                    }
                }
            }

            return dto;
        }

        public async Task<bool> ToggleRunningStatus(Guid id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null)
                throw new ArgumentException("Promoção não encontrada.");

            promo.Running = !promo.Running;

            _context.Promotions.Update(promo);
            await _context.SaveChangesAsync();
            return promo.Running;
        }

        public async Task<PromotionResponseDto> UpdatePromotion(Guid id, PromotionRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var promotion = await _context.Promotions
                .Include(p => p.PromotionModifiers)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promotion == null)
                throw new ArgumentException("Promoção não encontrada.");

            promotion.Name = dto.Name;
            promotion.Description = dto.Description;
            promotion.BaseProductId = dto.BaseProductId;
            promotion.PromotionalPrice = dto.PromotionalPrice;

            _context.PromotionModifiers.RemoveRange(promotion.PromotionModifiers);

            var newModifiers = dto.PromotionModifiers.Select(mod => new PromotionModifier
            {
                Id = Guid.NewGuid(),
                ModifierOptionId = mod.ModifierOptionId,
                OverridePrice = mod.OverridePrice,
                PromotionId = promotion.Id
            }).ToList();

            foreach (var mod in newModifiers)
            {
                promotion.PromotionModifiers.Add(mod);
                _context.PromotionModifiers.Add(mod);
            }

            _context.Promotions.Update(promotion);
            await _context.SaveChangesAsync();

            return await GetPromotionById(promotion.Id);
        }

        public async Task<bool> DeletePromotion(Guid id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null)
                throw new ArgumentException("Promoção não encontrada.");

            _context.Promotions.Remove(promo);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}