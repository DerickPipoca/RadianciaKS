using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace RadianciaKS.Application.Mappers
{
    [Mapper]
    public partial class CashShiftMapper
    {
        [MapperIgnoreSource(nameof(CashShift.TenantId))]
        [MapperIgnoreSource(nameof(CashShift.Active))]
        [MapperIgnoreSource(nameof(CashShift.EmployeeOpener))]
        [MapperIgnoreSource(nameof(CashShift.EmployeeCloser))]
        [MapperIgnoreSource(nameof(CashShift.Orders))]
        [MapperIgnoreTarget(nameof(CashShiftResponseDto.PendingOrdersCount))]
        public partial CashShiftResponseDto ToDto(CashShift cashShift);

        [MapperIgnoreTarget(nameof(CashShift.Id))]
        [MapperIgnoreTarget(nameof(CashShift.TenantId))]
        [MapperIgnoreTarget(nameof(CashShift.Active))]
        [MapperIgnoreTarget(nameof(CashShift.CreatedAt))]
        [MapperIgnoreTarget(nameof(CashShift.EmployeeOpener))]
        [MapperIgnoreTarget(nameof(CashShift.EmployeeCloser))]
        [MapperIgnoreSource(nameof(CashShiftResponseDto.CreatedAt))]
        [MapperIgnoreSource(nameof(CashShiftResponseDto.Id))]
        [MapperIgnoreTarget(nameof(CashShift.Orders))]
        [MapperIgnoreSource(nameof(CashShiftResponseDto.PendingOrdersCount))]
        public partial CashShift ToEntity(CashShiftResponseDto cashShiftDto);
    }
}