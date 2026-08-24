import { CashShiftStatus } from '../enums/cash-shift-status';

export interface CashShiftResponse {
  id: string;
  initialBalance: number;
  finalCalculatedBalance?: number;
  finalReportedBalance?: number;
  status: CashShiftStatus;
  createdAt: string;
  closedAt?: string;
  employeeOpenerId: string;
  employeeCloserId?: string;
}

export interface OpenCashShiftRequest {
  initialBalance: number;
}

export interface CloseCashShiftRequest {
  finalReportedBalance: number;
}

export interface CashShiftHistory {
  cashShiftId: string;
  openedAt: string;
  closedAt?: string;
  label: string;
  status: CashShiftStatus | number;
  totalRevenue: number;
}
