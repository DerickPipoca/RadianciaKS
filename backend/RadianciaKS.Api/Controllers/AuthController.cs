using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RadianciaKS.Application.DTOs.Auth;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public AuthController(IApplicationDbContext context, IPasswordService passwordService, IConfiguration configuration)
        {
            _context = context;
            _passwordService = passwordService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var employee = await _context.Employees
                            .FirstOrDefaultAsync(e => e.CPF == dto.CPF && e.Active);
            if (employee == null)
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Falha na Autenticação",
                    Detail = "CPF ou senha incorretos."
                });

            var isPasswordValid = _passwordService.VerifyPassword(dto.Password, employee.PasswordHash);

            if (!isPasswordValid)
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Falha na Autenticação",
                    Detail = "CPF ou senha incorretos."
                });

            var token = GenerateJwtToken(employee);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Name = employee.Name,
                Role = employee.Role.ToString()
            });
        }

        private string GenerateJwtToken(Employee employee)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["Secret"]!);

            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, employee.Id.ToString()),
                new(ClaimTypes.Name, employee.Name),
                new(ClaimTypes.Role, employee.Role.ToString()),
                new("TenantId", employee.TenantId.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}