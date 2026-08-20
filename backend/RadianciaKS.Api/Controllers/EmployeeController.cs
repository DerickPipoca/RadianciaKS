using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class EmployeeController : ControllerBase
    {
        private readonly IEmployeeService _employeeService; 

        public EmployeeController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeRequestDto dto)
        {
            var employee = await _employeeService.CreateEmployee(dto);
            return CreatedAtAction(nameof(CreateEmployee), new { id = employee.Id }, employee);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employee = await _employeeService.GetAllEmployees();

            return Ok(employee);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmployeeById(Guid id)
        {
            var employee = await _employeeService.GetBasicInformationEmployeeById(id);

            return Ok(employee);
        }

        [HttpGet("admin/{id}")]
        public async Task<IActionResult> GetEmployeeDetailsById(Guid id)
        {
            var employee = await _employeeService.GetEmployeeById(id);

            return Ok(employee);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] EmployeeUpdateDto dto)
        {
            var employee = await _employeeService.UpdateEmployee(id, dto);

            return Ok(employee);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(Guid id)
        {
            await _employeeService.DeleteEmployee(id);

            return NoContent();
        }
    }
}