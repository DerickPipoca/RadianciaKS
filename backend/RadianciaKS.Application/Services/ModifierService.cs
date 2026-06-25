using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services
{
    public class ModifierService : IModifierService
    {
        private readonly IApplicationDbContext _context;

        public ModifierService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ModifierOptionResponseDto> AddOptionToGroupAsync(Guid groupId, ModifierOptionRequestDto dto)
        {
            var group = await _context.ModifierGroups.FindAsync(groupId);
            if (group == null) throw new ArgumentException("Grupo de modificadores não encontrado.");

            var option = new ModifierOption
            {
                ModifierGroupId = groupId,
                Name = dto.Name,
                AdditionalPrice = dto.AdditionalPrice,
                Description = dto.Description
            };

            _context.ModifierOptions.Add(option);
            await _context.SaveChangesAsync();

            return new ModifierOptionResponseDto
            {
                Id = option.Id,
                Name = option.Name,
                AdditionalPrice = option.AdditionalPrice
            };
        }

        public async Task<ModifierGroupResponseDto> CreateGroupAsync(ModifierGroupRequestDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) throw new ArgumentException("Produto não encontrado.");

            var group = new ModifierGroup
            {
                ProductId = dto.ProductId,
                Name = dto.Name,
                MinChoices = dto.MinChoices,
                MaxChoices = dto.MaxChoices
            };

            _context.ModifierGroups.Add(group);
            await _context.SaveChangesAsync();

            return MapToGroupDto(group);
        }

        public async Task DeleteGroupAsync(Guid groupId)
        {
            var group = await _context.ModifierGroups.FindAsync(groupId);
            if (group == null) throw new ArgumentException("Grupo não encontrado.");

            _context.ModifierGroups.Remove(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOptionAsync(Guid optionId)
        {
            var option = await _context.ModifierOptions.FindAsync(optionId);
            if (option == null) throw new ArgumentException("Opção não encontrada.");

            _context.ModifierOptions.Remove(option);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ModifierGroupResponseDto>> GetGroupsByProductAsync(Guid productId)
        {
            var groups = await _context.ModifierGroups
                .Include(g => g.Options)
                .Where(g => g.ProductId == productId)
                .ToListAsync();

            return groups.Select(MapToGroupDto);
        }

        private static ModifierGroupResponseDto MapToGroupDto(ModifierGroup group)
        {
            return new ModifierGroupResponseDto
            {
                Id = group.Id,
                Name = group.Name,
                MinChoices = group.MinChoices,
                MaxChoices = group.MaxChoices,
                Options = group.Options?.Select(o => new ModifierOptionResponseDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    AdditionalPrice = o.AdditionalPrice,
                    Description = o.Description
                }).ToList() ?? new List<ModifierOptionResponseDto>()
            };
        }
    }
}