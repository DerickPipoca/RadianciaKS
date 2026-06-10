using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services.Auth
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string providedPassword, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, passwordHash);
        }
    }
}