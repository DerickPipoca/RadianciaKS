using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Auth;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public AuthService(IApplicationDbContext context, IPasswordService passwordService, ITokenService tokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto dto)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.CPF == dto.CPF && e.Active);

            if (employee == null)
                throw new UnauthorizedAccessException("CPF ou senha incorretos.");

            var isPasswordValid = _passwordService.VerifyPassword(dto.Password, employee.PasswordHash);

            if (!isPasswordValid)
                throw new UnauthorizedAccessException("CPF ou senha incorretos.");

            var token = _tokenService.GenerateToken(employee);

            return new LoginResponseDto
            {
                Token = token,
                Name = employee.Name,
                Role = employee.Role.ToString()
            };
        }
    }
}