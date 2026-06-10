using RadianciaKS.Application.DTOs.Employee;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IEmployeeService
    {
        Task<EmployeeResponseDto> CreateEmployee(EmployeeRequestDto dto);
        Task<IEnumerable<EmployeeBasicResponseDto>> GetAllEmployees();
        Task<EmployeeBasicResponseDto> GetBasicInformationEmployeeById(Guid id);
        Task<EmployeeResponseDto> GetEmployeeById(Guid id);
        Task<EmployeeResponseDto> UpdateEmployee(Guid id, EmployeeUpdateDto dto);
        Task DeleteEmployee(Guid id);
    }
}