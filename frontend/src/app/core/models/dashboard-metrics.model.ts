import { CashShiftStatus } from '../enums/cash-shift-status';
import { EmployeeRole } from '../enums/employee-role';

export interface TopSellingItem {
  productName: string;
  quantitySold: number;
  transactionsCount: number;
}

export interface HistoryComparison {
  hasPreviousShift: boolean;
  revenuePercentage: number;
  ordersPercentage: number;
}

export interface TeamProductivity {
  employeeName: string;
  employeeRole: EmployeeRole | string;
  completedTasks: number;
}

export interface CashFlow {
  paymentMethod: string;
  totalAmount: number;
  transactionsCount: number;
}

export interface SalesChart {
  label: string;
  value: number;
}

export interface DashboardMetrics {
  totalRevenue: number;
  averageTicket: number;
  totalOrders: number;

  openedByName: string;
  closedByName?: string;

  initialBalance: number;
  finalCalculatedBalance?: number;
  finalReportedBalance?: number;
  shiftStatus: CashShiftStatus;

  canceledOrdersCount: number;
  canceledOrdersAmount: number;

  previousShiftComparison: HistoryComparison;

  topSellingItems: TopSellingItem[];
  cashFlow: CashFlow[];
  salesChart: SalesChart[];
  waiterProductivity: TeamProductivity[];
}
