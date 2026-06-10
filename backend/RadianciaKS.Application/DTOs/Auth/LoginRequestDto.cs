using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadianciaKS.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string CPF { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}