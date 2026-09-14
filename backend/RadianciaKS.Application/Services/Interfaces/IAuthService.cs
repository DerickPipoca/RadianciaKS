using RadianciaKS.Application.DTOs.Auth;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> Login(LoginRequestDto dto);
    }
}