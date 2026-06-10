using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class EmployeeMapper
    {
        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("PasswordHash")]
        public partial EmployeeResponseDto ToDto(Employee employee);

        [MapperIgnoreSource("TenantId")]
        [MapperIgnoreSource("CreatedAt")]
        [MapperIgnoreSource("Active")]
        [MapperIgnoreSource("PasswordHash")]
        [MapperIgnoreSource("Birthday")]
        [MapperIgnoreSource("CPF")]
        public partial EmployeeBasicResponseDto ToBasicDto(Employee employee);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreSource("Password")]
        [MapperIgnoreTarget("PasswordHash")]
        public partial Employee ToEntity(EmployeeRequestDto dto);

        [MapperIgnoreTarget("Id")]
        [MapperIgnoreTarget("TenantId")]
        [MapperIgnoreTarget("CreatedAt")]
        [MapperIgnoreTarget("Active")]
        [MapperIgnoreSource("Password")]
        [MapperIgnoreTarget("PasswordHash")]
        public partial Employee UpdateToEntity(EmployeeUpdateDto dto);
    }
}