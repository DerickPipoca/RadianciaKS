using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IValidator<EmployeeRequestDto> _validator;
        private readonly IValidator<EmployeeUpdateDto> _updateValidator;
        private readonly EmployeeMapper _mapper;

        public EmployeeService(IApplicationDbContext context, IPasswordService passwordService, IValidator<EmployeeRequestDto> validator, IValidator<EmployeeUpdateDto> updateValidator)
        {
            _context = context;
            _passwordService = passwordService;
            _validator = validator;
            _updateValidator = updateValidator;
            _mapper = new EmployeeMapper();
        }

        public async Task<EmployeeResponseDto> CreateEmployee(EmployeeRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            var employeeToAdd = _mapper.ToEntity(dto);
            string? password = EncryptPassword(dto.Password);
            employeeToAdd.PasswordHash = password!;
            var employee = await _context.Employees.AddAsync(employeeToAdd);
            await _context.SaveChangesAsync();
            return _mapper.ToDto(employee.Entity);
        }

        public async Task DeleteEmployee(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                throw new ArgumentException("O empregado informado não existe.");

            employee.Active = false;

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<EmployeeBasicResponseDto>> GetAllEmployees()
        {
            var employees = await _context.Employees.ToListAsync();
            return employees.Select(c => _mapper.ToBasicDto(c));
        }

        public async Task<EmployeeBasicResponseDto> GetBasicInformationEmployeeById(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                throw new ArgumentException("O empregado informado não existe.");

            return _mapper.ToBasicDto(employee);
        }

        public async Task<EmployeeResponseDto> GetEmployeeById(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                throw new ArgumentException("O empregado informado não existe.");

            return _mapper.ToDto(employee);
        }

        public async Task<EmployeeResponseDto> UpdateEmployee(Guid id, EmployeeUpdateDto dto)
        {
            await _updateValidator.ValidateAndThrowAsync(dto);
            var employe = await _context.Employees.FindAsync(id);
            if (employe == null)
                throw new ArgumentException("A categoria informada não existe.");

            string? password = EncryptPassword(dto.Password);

            employe.Update(
                dto.Name,
                dto.Birthday,
                dto.CPF,
                dto.Role,
                password
            );

            await _context.SaveChangesAsync();
            return _mapper.ToDto(employe);
        }

        private string? EncryptPassword(string? password)
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                string passwordHash = _passwordService.HashPassword(password);
                return passwordHash;
            }
            return null;
        }
    }
}