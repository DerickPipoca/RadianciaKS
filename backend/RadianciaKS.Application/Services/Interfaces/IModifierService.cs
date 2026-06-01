using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadianciaKS.Application.DTOs.Modifier;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IModifierService
    {
        Task<ModifierGroupResponseDto> CreateGroupAsync(ModifierGroupRequestDto dto);
        Task<ModifierOptionResponseDto> AddOptionToGroupAsync(Guid groupId, ModifierOptionRequestDto dto);
        Task<IEnumerable<ModifierGroupResponseDto>> GetGroupsByProductAsync(Guid productId);
        Task DeleteGroupAsync(Guid groupId);
        Task DeleteOptionAsync(Guid optionId);
    }
}