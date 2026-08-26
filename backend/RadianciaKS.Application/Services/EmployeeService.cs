using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IValidator<EmployeeRequestDto> _validator;
        private readonly IValidator<EmployeeUpdateDto> _updateValidator;
        private readonly EmployeeMapper _mapper;
        private readonly IUserProvider _userProvider;

        public EmployeeService(IApplicationDbContext context, IPasswordService passwordService, IValidator<EmployeeRequestDto> validator, IValidator<EmployeeUpdateDto> updateValidator, IUserProvider userProvider)
        {
            _context = context;
            _passwordService = passwordService;
            _validator = validator;
            _updateValidator = updateValidator;
            _mapper = new EmployeeMapper();
            _userProvider = userProvider;
        }

        public async Task<EmployeeResponseDto> CreateEmployee(EmployeeRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            EnsureRoleAssignmentPermission(dto.Role);

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
                throw new ArgumentException("O funcionário informado não existe.");

            EnsureTargetPermission(employee);

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
                throw new ArgumentException("O funcionário informado não existe.");

            return _mapper.ToBasicDto(employee);
        }

        public async Task<EmployeeResponseDto> GetEmployeeById(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                throw new ArgumentException("O funcionário informado não existe.");

            return _mapper.ToDto(employee);
        }

        public async Task<EmployeeResponseDto> UpdateEmployee(Guid id, EmployeeUpdateDto dto)
        {
            await _updateValidator.ValidateAndThrowAsync(dto);
            var employe = await _context.Employees.FindAsync(id);
            if (employe == null)
                throw new ArgumentException("O funcionário informado não existe.");

            EnsureTargetPermission(employe);
            EnsureRoleAssignmentPermission(dto.Role);

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

        private void EnsureTargetPermission(Domain.Models.Employee targetEmployee)
        {
            var currentUserId = _userProvider.GetUserId();
            var currentUserRoleStr = _userProvider.GetUserRole();

            if (targetEmployee.Id == _userProvider.GetUserId())
                throw new ArgumentException("Não é possível alterar a conta logada pela mesma.");

            if (string.IsNullOrEmpty(currentUserRoleStr) || !Enum.TryParse<EmployeeRole>(currentUserRoleStr, out var currentUserRole))
                throw new ArgumentException("Usuário não autenticado ou com cargo inválido.");

            if (currentUserRole == EmployeeRole.Admin) return;

            if (currentUserRole == EmployeeRole.Manager)
            {
                if (targetEmployee.Role == EmployeeRole.Admin)
                    throw new ArgumentException("Gerentes não podem alterar ou excluir Administradores.");

                if (targetEmployee.Role == EmployeeRole.Manager && targetEmployee.Id != currentUserId)
                    throw new ArgumentException("Gerentes não podem alterar ou excluir outros Gerentes.");
            }
        }

        private void EnsureRoleAssignmentPermission(EmployeeRole newRole)
        {
            var currentUserRoleStr = _userProvider.GetUserRole();

            if (string.IsNullOrEmpty(currentUserRoleStr) || !Enum.TryParse<EmployeeRole>(currentUserRoleStr, out var currentUserRole))
                throw new ArgumentException("Usuário não autenticado ou com cargo inválido.");

            if (currentUserRole == EmployeeRole.Admin) return;

            if (currentUserRole == EmployeeRole.Manager && newRole == EmployeeRole.Admin)
            {
                throw new ArgumentException("Apenas Administradores podem conceder permissão de Administrador.");
            }
        }
    }
}