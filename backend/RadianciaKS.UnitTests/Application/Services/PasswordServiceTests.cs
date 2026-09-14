using RadianciaKS.Application.Services.Auth;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _passwordService;

        public PasswordServiceTests()
        {
            _passwordService = new PasswordService();
        }

        [Fact]
        public void HashPassword_Should_ReturnValidBcryptHash_When_PasswordProvided()
        {
            const string rawPassword = "MinhaSenhaSuperSegura!123";

            var hash = _passwordService.HashPassword(rawPassword);

            hash.ShouldNotBeNullOrWhiteSpace();
            hash.ShouldNotBe(rawPassword);
            hash.StartsWith("$2").ShouldBeTrue();
        }

        [Fact]
        public void HashPassword_Should_GenerateDifferentHashes_ForSamePasswordDueToSalt()
        {
            const string password = "SenhaComum@2026";

            var hash1 = _passwordService.HashPassword(password);
            var hash2 = _passwordService.HashPassword(password);

            hash1.ShouldNotBe(hash2);
        }

        [Fact]
        public void VerifyPassword_Should_ReturnTrue_When_PasswordMatchesHash()
        {
            const string rawPassword = "SenhaCorreta#123";
            var hash = _passwordService.HashPassword(rawPassword);

            var isValid = _passwordService.VerifyPassword(rawPassword, hash);

            isValid.ShouldBeTrue();
        }

        [Theory]
        [InlineData("SenhaErrada#123")]
        [InlineData("senhacorreta#123")]
        [InlineData("")]
        [InlineData(" ")]
        public void VerifyPassword_Should_ReturnFalse_When_PasswordDoesNotMatchHash(string wrongPassword)
        {
            const string originalPassword = "SenhaCorreta#123";
            var hash = _passwordService.HashPassword(originalPassword);

            var isValid = _passwordService.VerifyPassword(wrongPassword, hash);

            isValid.ShouldBeFalse();
        }
    }
}