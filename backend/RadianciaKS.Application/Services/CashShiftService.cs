using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.DTOs.DashboardMetrics;
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
        private readonly ITenantProvider _tenantProvider;
        private readonly IKdsNotificationService _notificationService;

        public CashShiftService(IApplicationDbContext context, IUserProvider userProvider, ITenantProvider tenantProvider, IKdsNotificationService notificationService)
        {
            _context = context;
            _userProvider = userProvider;
            _tenantProvider = tenantProvider;
            _notificationService = notificationService;
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

            var tenantId = _tenantProvider.GetTenantId().ToString();
            await _notificationService.UpdateCashShiftStatusAsync(tenantId, CashShiftStatus.Closed);

            return _mapper.ToDto(openShift);
        }

        public async Task<CashShiftResponseDto?> GetCurrentOpenShift()
        {
            var shift = await _context.CashShifts
                        .FirstOrDefaultAsync(c => c.Status == CashShiftStatus.Open);

            return shift == null ? null : _mapper.ToDto(shift);
        }


        public async Task<PagedResponse<CashShiftHistoryDto>> GetCashShiftHistoryAsync(BaseQueryParameters queryParameters)
        {
            var query = _context.CashShifts
                .Include(c => c.Orders)
                .OrderByDescending(c => c.CreatedAt)
                .AsQueryable();

            var totalRecords = await query.CountAsync();

            var shifts = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            var data = shifts.Select(s => new CashShiftHistoryDto
            {
                CashShiftId = s.Id,
                OpenedAt = s.CreatedAt,
                ClosedAt = s.ClosedAt,
                Status = s.Status,
                TotalRevenue = s.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount)
            }).ToList();

            return new PagedResponse<CashShiftHistoryDto>
            {
                Data = data,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                TotalRecords = totalRecords
            };
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

            var tenantId = _tenantProvider.GetTenantId().ToString();
            await _notificationService.UpdateCashShiftStatusAsync(tenantId, CashShiftStatus.Open);

            return _mapper.ToDto(shift);
        }
    }
}