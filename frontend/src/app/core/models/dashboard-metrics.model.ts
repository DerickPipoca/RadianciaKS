export interface TopSellingItem {
  productName: string;
  quantitySold: number;
}

export interface CashFlow {
  paymentMethod: string;
  totalAmount: number;
}

export interface SalesChart {
  label: string;
  value: number;
}

export interface DashboardMetrics {
  totalRevenue: number;
  averageTicket: number;
  totalOrders: number;
  topSellingItems: TopSellingItem[];
  cashFlow: CashFlow[];
  salesChart: SalesChart[];
}