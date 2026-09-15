using System.Net.Http.Headers;
using System.Net.Http.Json;
using RadianciaKS.Application.DTOs.Auth;

namespace RadianciaKS.IntegrationTests.Helpers
{
    public static class HttpClientExtensions
    {
        public static async Task AuthenticateAsAsync(this HttpClient client, string cpf, string password)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                CPF = cpf,
                Password = password
            });

            response.EnsureSuccessStatusCode();

            var loginResult = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult!.Token);
        }
    }
}