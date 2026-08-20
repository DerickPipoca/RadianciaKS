using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services
{
    public class CashShiftService : ICashShiftService
    {
        private readonly IApplicationDbContext _context;
        private readonly IUserProvider _userProvider;
        private readonly CashShiftMapper _mapper;

        public CashShiftService(IApplicationDbContext context, IUserProvider userProvider)
        {
            _context = context;
            _userProvider = userProvider;
            _mapper = new CashShiftMapper();
        }

        public async Task<CashShiftResponseDto> CloseShift(CloseCashShiftDto dto)
        {
            var openShift = await _context.CashShifts
            .Include(c => c.Orders)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(c => c.Status == CashShiftStatus.Open && c.Active);

            if (openShift == null)
                throw new Exception("Não há caixa aberto para ser fechado.");

            decimal totalSales = openShift.Orders
                .Where(p => p.PaymentStatus == PaymentStatus.Paid)
                .SelectMany(o => o.Payments)
                .Sum(p => p.Amount);

            openShift.FinalCalculatedBalance = openShift.InitialBalance + totalSales;
            openShift.FinalReportedBalance = dto.FinalReportedBalance;
            openShift.Status = CashShiftStatus.Closed;
            openShift.ClosedAt = DateTime.UtcNow;
            openShift.EmployeeCloserId = _userProvider.GetUserId();

            await _context.SaveChangesAsync();

            return _mapper.ToDto(openShift);
        }

        public async Task<CashShiftResponseDto?> GetCurrentOpenShift()
        {
            var shift = await _context.CashShifts
                        .FirstOrDefaultAsync(c => c.Status == CashShiftStatus.Open);

            return shift == null ? null : _mapper.ToDto(shift);
        }

        public async Task<CashShiftResponseDto> OpenShift(OpenCashShiftDto dto)
        {
            var hasOpenShift = await _context.CashShifts.AnyAsync(c => c.Status == CashShiftStatus.Open && c.Active);
            if (hasOpenShift)
                throw new Exception("Já existe um caixa aberto para este estabelecimento.");

            var employeeOpenerId = _userProvider.GetUserId();

            if (employeeOpenerId == null)
                throw new Exception("ID do empregado não encontrado.");

            var shift = new CashShift
            {
                InitialBalance = dto.InitialBalance,
                Status = CashShiftStatus.Open,
                EmployeeOpenerId = (Guid)employeeOpenerId
            };

            _context.CashShifts.Add(shift);
            await _context.SaveChangesAsync();

            return _mapper.ToDto(shift);
        }
    }
}